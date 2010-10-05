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
        { $_ -eq 'New-Package' -or $_ -eq 'nnp' } {
            $choices = _TabExpansionForNewPackage $secondLastToken $tokens.length $filter
        }
    
        { $_ -eq 'Install-Package' -or $_ -eq 'nip' } {
            $choices = _TabExpansionForAddPackage $secondLastToken $tokens.length $filter
        }

        { $_ -eq 'Uninstall-Package' -or $_ -eq 'nup' } {
            $choices = _TabExpansionForRemovePackage $secondLastToken $tokens.length $filter
        }

        { $_ -eq 'Update-Package' -or $_ -eq 'npp' } {
            $choices = _TabExpansionForRemovePackage $secondLastToken $tokens.length $filter
        }
    }
    
    if($choices) {
        # Return all the choices, do some filtering based on the last word and sort them
        $choices | Where-Object { $_.StartsWith($filter, "OrdinalIgnoreCase") } | Sort-Object
    }
    else {
        # Fallback the to default tab expansion
        DefaultTabExpansion $line $lastWord 
    }
}

function _TabExpansionForNewPackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 2 -and !$secondLastWord.StartsWith('-'))) {
        Get-Projects
    }
}

function _TabExpansionForAddPackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        (Get-Package) | Group-Object ID | ForEach-Object { $_.Name }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        Get-Projects
    }
}

function _TabExpansionForRemovePackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        (Get-Package -Installed) | Group-Object ID | ForEach-Object { $_.Name }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        Get-Projects
    }
}

# hook up Solution Opened even to execute init.ps1 script files when a new solution is opened.

$global:solutionEvents = Get-Interface $dte.Events.SolutionEvents ([EnvDTE._dispSolutionEvents_Event])

$global:solutionEvents.add_Opened([EnvDTE._dispSolutionEvents_OpenedEventHandler]{
    _ExecuteInitScripts
})

function global:_AddToolsFolderToEnv([string]$rootPath) {
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

function global:_ExecuteScript([string]$rootPath, [string]$scriptFile, $package) {
    $fullPath = (Join-Path $rootPath $scriptFile)
    if (Test-Path $fullPath) {
        $folder = Split-Path $fullPath
        & $fullPath $rootPath $folder $package
    }
}

function global:_ExecuteInitScripts() {
    $packageManager = [NuPack.VisualStudio.VsPackageManager]::GetPackageManager([object]$dte)
    $repository = $packageManager.LocalRepository
    $localPackages = $repository.GetPackages()

    $localPackages | ForEach-Object {
        $path = $packageManager.PathResolver.GetInstallPath($_)

        _AddToolsFolderToEnv $path
        _ExecuteScript $path "tools\init.ps1" $_
    }
}

if ($dte -and $dte.Solution -and $dte.Solution.IsOpen) {
    _ExecuteInitScripts
}

# assign aliases to package cmdlets

New-Alias 'nnp' 'New-Package'
New-Alias 'nep' 'Get-Package'
New-Alias 'nip' 'Install-Package'
New-Alias 'nup' 'Uninstall-Package'
New-Alias 'npp' 'Update-Package'

New-Alias 'List-Package' 'Get-Package'