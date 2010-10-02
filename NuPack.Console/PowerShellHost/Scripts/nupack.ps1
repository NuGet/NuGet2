# make sure we stop on exceptions
$ErrorActionPreference = "Stop"

# Get the solution events
$global:packageManagerInitialized = $false
$global:solutionEvents = Get-Interface $dte.Events.SolutionEvents ([EnvDTE._dispSolutionEvents_Event])

$global:solutionEvents.add_Opened([EnvDTE._dispSolutionEvents_OpenedEventHandler]{
    $packageManager = _GetPackageManager
    $repository = $packageManager.LocalRepository
    $localPackages = $repository.GetPackages()

    $localPackages | ForEach-Object {
        $path = $packageManager.PathResolver.GetInstallPath($_)

        _AddToolsFolderToEnv $path
        _ExecuteScript $path "tools\init.ps1" $_
    }
})

# Backup the original tab expansion function
if ((Test-Path Function:\DefaultTabExpansion) -eq $false) {
    Rename-Item Function:\TabExpansion global:DefaultTabExpansion
}

function global:doit() {
    Write-Host "This is incredible."	
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
    
        { $_ -eq 'Add-Package' -or $_ -eq 'nap' -or $_ -eq 'Install-Package' } {
            $choices = _TabExpansionForAddPackage $secondLastToken $tokens.length $filter
        }

        { $_ -eq 'Remove-Package' -or $_ -eq 'nrp' -or $_ -eq 'Uninstall-Package' } {
            $choices = _TabExpansionForRemovePackage $secondLastToken $tokens.length $filter
        }

        { $_ -eq 'Update-Package' -or $_ -eq 'nup' } {
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
        GetProjectNames
    }
}

function _TabExpansionForAddPackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        List-Package | Group-Object ID | ForEach-Object { $_.Name }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        GetProjectNames
    }
}

function _TabExpansionForRemovePackage([string]$secondLastWord, [int]$tokenCount, [string]$filter) {
    if ($filter.StartsWith('-')) {
       # if this is a parameter, do not return anything so that the default PS tab expansion can supply the list of parameters
    }
    elseif (($secondLastWord -eq '-id') -or ($secondLastWord -eq '')) {
        (List-Package -Installed) | Group-Object ID | ForEach-Object { $_.Name }
    }
    elseif (($secondLastWord -eq '-project') -or 
            ($tokenCount -eq 3 -and !$secondLastWord.StartsWith('-'))) {
        GetProjectNames
    }
}

function global:_GetProjectManager($packageManager, $projectName) {
    $project = Get-Project $projectName
    $projectManager = $packageManager.GetProjectManager($project)

    if(!$projectManager.Logger) {
        # Initialize the project manager properties if they are set
        $projectManager.Logger = _CreateLogger

        # REVIEW: We really want to do this once per project manager instance
        $projectManager.add_PackageReferenceAdded({ 
            param($sender, $e)
            
            Write-Verbose "Executing install script after adding package $($e.Package.Id)..."
            _ExecuteScript $e.InstallPath "tools\install.ps1" $e.Package $project
        }.GetNewClosure());

        $projectManager.add_PackageReferenceRemoving({
            param($sender, $e)

            Write-Verbose "Executing uninstall script before removing package $($e.Package.Id)..."
            _ExecuteScript $e.InstallPath "tools\uninstall.ps1" $e.Package $project
        }.GetNewClosure());
    }

    return $projectManager;
}

function global:_GetPackageManager {
    if(!$dte) {
        throw "DTE isn't loaded"
    }

    # Create a visual studio package manager
    $packageManager = [NuPack.VisualStudio.VsPackageManager]::GetPackageManager([object]$dte)

    if(!$global:packageManagerInitialized) {
        # Add an event for when packages are installed
        $packageManager.add_PackageInstalling($function:_OnPackageInstalling)
        $packageManager.add_PackageInstalled($function:_OnPackageInstalled)
        $packageManager.add_PackageUninstalling($function:_OnPackageUninstalling)

        $global:packageManagerInitialized = $true
    }

    return $packageManager
}

function global:_OnPackageInstalling($sender, $e) {
    _WriteDisclaimer $e.Package
}

function global:_OnPackageInstalled($sender, $e) {

    $path = $e.TargetPath

    _AddToolsFolderToEnv $path
    
    Write-Verbose "Executing init script after installing package $($e.Package.Id)..."
    _ExecuteScript $path "tools\init.ps1"
}

function global:_OnPackageUninstalling($sender, $e) {    
    # TODO: remove tools path from the environment variable
}

function global:_ExecuteScript([string]$rootPath, [string]$scriptFile, $package, $project) {
    $fullPath = (Join-Path $rootPath $scriptFile)
        
    if (Test-Path $fullPath) {
        $folder = Split-Path $fullPath
        & $fullPath $rootPath $folder $package $project
    }
}

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

# assign aliases to package cmdlets

New-Alias 'nnp' 'New-Package'
New-Alias 'nlp' 'Get-Package'
New-Alias 'nap' 'Add-Package'
New-Alias 'nrp' 'Remove-Package'
New-Alias 'nup' 'Update-Package'

New-Alias 'List-Package' 'Get-Package'
New-Alias 'Install-Package' 'Add-Package'
New-Alias 'Uninstall-Package' 'Remove-Package'