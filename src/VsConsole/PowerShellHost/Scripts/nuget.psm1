# make sure we stop on exceptions
$ErrorActionPreference = "Stop"

# This object reprents the result value for tab expansion functions when no result is returned.
# This is so that we can distinguish it from $null, which has different semantics
$NoResultValue = New-Object PSObject -Property @{ NoResult = $true }

function NugetTabExpansion($line, $lastWord) {
    $filter = $lastWord.Trim()
    
    if ($filter.StartsWith('-')) {
       # if this is a parameter, let default PS tab expansion supply the list of parameters
       return (DefaultTabExpansion $line $lastWord)
    }
    
    # remove double quotes around last word
    $trimmedFilter = $filter.Trim( '"', "'" )
    if ($trimmedFilter.length -lt $filter.length) {
        $filter = $trimmedFilter
        $addQuote = $true
    }
    
    $tokens = $line.Split(@(' '), 'RemoveEmptyEntries')
    if (!$filter) {
        $tokens = $tokens + $filter
    }

    if ($tokens.length -gt 2) {
        $secondLastToken = $tokens[-2]
    }
    else {
        $secondLastToken = ''
    }
    
    switch ($tokens[0]) {
        'New-Package' {
            $choices = TabExpansionForNewPackage $secondLastToken $tokens.length
        }
    
        'Install-Package' {
            $choices = TabExpansionForAddPackage $line $secondLastToken $tokens.length $filter
        }

        'Uninstall-Package' {
            $choices = TabExpansionForRemovePackage $secondLastToken $tokens.length $filter
        }

        'Update-Package' {
            $choices = TabExpansionForRemovePackage $secondLastToken $tokens.length $filter
        }
        
        'Get-Project' {
            $choices = TabExpansionForGetProject $secondLastToken
        }
        
        default {
            $choices = $NoResultValue
        }
    }
    
    if ($choices -eq $NoResultValue) {
        return $choices
    }
    elseif ($choices) {
        # Return all the choices, do some filtering based on the last word, sort them and wrap each suggestion in a double quote if necessary
        $choices | 
            Where-Object { $_.StartsWith($filter, "OrdinalIgnoreCase") } | 
            Sort-Object |
            ForEach-Object { if ($addQuote -or $_.IndexOf(' ') -gt -1) { "'" + ($_ -replace "'", "''") + "'"} else { $_ } }
    }
    else {
        # return null here will tell the console not to show system file paths
        return $null
    }
}

function TabExpansionForNewPackage([string]$secondLastWord, [int]$tokenCount) {
    if (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 2 -and !$secondLastWord.StartsWith('-'))) {
        GetProjectNames
    }
    else {
        return $NoResultValue
    }
}

function TabExpansionForAddPackage([string]$line, [string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        # Determine if a Source param is present
        $source = ""
        if ($line -match "-Source(\s+)([^\s]+)") {
            $source = $matches[2]
            Find-Package -Remote -Source $source -Filter $filter -ea 'SilentlyContinue' | Group-Object ID | ForEach-Object { $_.Name }
        }
        else {
            Find-Package -Remote -Filter $filter -ea 'SilentlyContinue' | Group-Object ID | ForEach-Object { $_.Name }
        }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        GetProjectNames
    }
    else {
        return $NoResultValue
    }
}

function TabExpansionForRemovePackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        if (IsSolutionOpen) {
            (Find-Package -Filter $filter -ea 'SilentlyContinue') | Group-Object ID | ForEach-Object { $_.Name }
        }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        GetProjectNames
    }
    else {
        return $NoResultValue
    }
}

function TabExpansionForGetProject([string]$secondLastWord) {
    if (($secondLastWord -eq '-name') -or ($secondLastWord -eq '')) {
        GetProjectNames
    }
    else {
        return $NoResultValue
    }
}

function GetProjectNames() {
    (Get-Project -All) | ForEach-Object { $_.Name }
}

# Hook up Solution events

$solutionEvents = Get-Interface $dte.Events.SolutionEvents ([EnvDTE._dispSolutionEvents_Event])

$solutionEvents.add_Opened([EnvDTE._dispSolutionEvents_OpenedEventHandler]{
    ExecuteInitScripts
    UpdateWorkingDirectory
})

$solutionEvents.add_AfterClosing([EnvDTE._dispSolutionEvents_AfterClosingEventHandler]{
    UpdateWorkingDirectory
})

function AddToolsFolderToEnv([string]$rootPath) {
    # add tools path to the environment
    $toolsPath = (Join-Path $rootPath 'tools')
    if (Test-Path $toolsPath) {
        if (!$env:path.EndsWith(';')) {
            $toolsPath = ';' + $toolsPath
        }
        # add the tools folder to the environment path
        $env:path = $env:path + $toolsPath
    }
}

function ExecuteScript([string]$rootPath, [string]$scriptFile, $package) {
    $fullPath = (Join-Path $rootPath $scriptFile)
    if (Test-Path $fullPath) {
        $folder = Split-Path $fullPath
        & $fullPath $rootPath $folder $package
    }
}

function ExecuteInitScripts() {
    $packageManager = $packageManagerFactory.CreatePackageManager()
    $repository = $packageManager.LocalRepository
    $localPackages = $repository.GetPackages()

    $localPackages | ForEach-Object {
        $path = $packageManager.PathResolver.GetInstallPath($_)

        AddToolsFolderToEnv $path
        ExecuteScript $path "tools\init.ps1" $_
    }
}

function UpdateWorkingDirectory {
    $SolutionDir = if($DTE -and $DTE.Solution -and $DTE.Solution.FullName) { Split-Path $DTE.Solution.FullName -Parent }
    if ($SolutionDir) {
        Set-Location $SolutionDir
    } 
    else {
        Set-Location $Env:USERPROFILE
    }
}

function IsSolutionOpen() {
   return ($dte -and $dte.Solution -and $dte.Solution.IsOpen)
}

# execute init.ps1 files in the current solution
if (IsSolutionOpen) {
    ExecuteInitScripts
    UpdateWorkingDirectory
}