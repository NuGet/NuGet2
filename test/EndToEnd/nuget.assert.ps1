# Nuget specific assert helpers

function Get-SolutionPackage {
    param(
        [parameter(Mandatory = $true)]
        [string]$Id,
        [string]$Version
    )
    
    # Get the package entries from the solution
    $packages = Get-Package | ?{ $_.Id -eq $Id }
    
    if($Version) {
        $actualVersion = [Version]::Parse($Version)
        $packages = $packages | ?{ $_.Version -eq $actualVersion }
    }
    
    $packages
}

function Get-ProjectRepository {
    param(
        [parameter(Mandatory = $true)]
        $Project
    )
    
    $packageManager = $host.PrivateData.packageManagerFactory.CreatePackageManager()    
    $fileSystem = New-Object NuGet.PhysicalFileSystem((Get-ProjectDir $Project))
    New-Object NuGet.PackageReferenceRepository($fileSystem, $packageManager.LocalRepository)
}

function Get-ProjectPackage {
    param(
        [parameter(Mandatory = $true)]
        $Project,
        [parameter(Mandatory = $true)]
        [string]$Id,
        [string]$Version
    )
    
    $repository = Get-ProjectRepository $Project
        
    # We can't call the nuget methods since powershell gets confused with overload resolution
    $packages = $repository.GetPackages() | ?{ $_.Id -eq $Id }    
    
    if($Version) {
        $actualVersion = [Version]::Parse($Version)
        $packages = $packages | ?{ $_.Version -eq $actualVersion }
    }
    
    $packages
}

function Assert-Package {
    param(
        [parameter(Mandatory = $true)]
        $Project,
        [parameter(Mandatory = $true)]
        [string]$Id,
        [string]$Version
    )
    
    # Check for existance on disk of packages.config
    Assert-PathExists (Join-Path (Get-ProjectDir $Project) packages.config)
    
    # Check for the project item
    Assert-NotNull (Get-ProjectItem $Project packages.config) "packages.config does not exist in $($Project.Name)"
    
    $repository = Get-ProjectRepository $Project
    
    Assert-NotNull $repository "Unable to find the project repository"
    
    if($Version) {
        $actualVersion = [Version]::Parse($Version)
    }
    
    Assert-NotNull ([NuGet.PackageRepositoryExtensions]::Exists($repository, $Id, $actualVersion)) "Package $Id $Version is not referenced in $($Project.Name)"
}

function Assert-SolutionPackage {
    param(
        [parameter(Mandatory = $true)]
        [string]$Id,
        [string]$Version
    )
    
    # Make sure the packages directory exists
    Assert-PathExists (Get-PackagesDir) "The packages directory doesn't exist"
    
    $packages = Get-SolutionPackage $Id $Version
    
    if(!$packages -or $packages.Count -eq 0) {
        Assert-Fail "Package $Id $Version does not exist at solution level"
    }
}

function Get-PackagesDir {
    # TODO: Handle when the package location changes
    Join-Path (Get-SolutionDir) packages
}
