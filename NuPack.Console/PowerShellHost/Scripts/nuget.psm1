# make sure we stop on exceptions
$ErrorActionPreference = "Stop"

# Backup the original tab expansion function
if ((Test-Path Function:\DefaultTabExpansion) -eq $false) {
    Rename-Item Function:\TabExpansion global:DefaultTabExpansion
}

function global:TabExpansion($line, $lastWord) {
    $tokens = $line.Split(@(' '), 'RemoveEmptyEntries')
    $filter = $lastWord.Trim()

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
            $choices = TabExpansionForNewPackage $secondLastToken $tokens.length $filter
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
            $choices = TabExpansionForGetProject $secondLastToken $tokens.length $filter
        }
    }
    
    if($choices) {
        # Return all the choices, do some filtering based on the last word and sort them
        $choices | Where-Object { $_.StartsWith($filter, "OrdinalIgnoreCase") } | Sort-Object
    }
    else {
        # Fallback to the default tab expansion
        DefaultTabExpansion $line $lastWord 
    }
}

function TabExpansionForNewPackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 2 -and !$secondLastWord.StartsWith('-'))) {
        Get-ProjectNames
    }
}

function TabExpansionForAddPackage([string]$line, [string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
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
        Get-ProjectNames
    }
}

function TabExpansionForRemovePackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        if (IsSolutionOpen) {
            (Find-Package -Filter $filter -ea 'SilentlyContinue') | Group-Object ID | ForEach-Object { $_.Name } 
        }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        Get-ProjectNames
    }
}

function TabExpansionForGetProject([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-name') -or ($secondLastWord -eq '')) {
        Get-ProjectNames
    }
}

function global:Get-ProjectNames() {
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

if (IsSolutionOpen) {
    ExecuteInitScripts
    UpdateWorkingDirectory
}

# assign aliases to package cmdlets

New-Alias 'List-Package' 'Get-Package'

# export public functions and aliases 
Export-ModuleMember -Function 'Get-ProjectNames','TabExpansion' -Alias 'List-Package'
