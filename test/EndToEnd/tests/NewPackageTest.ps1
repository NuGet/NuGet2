
# Test for New-Package cmdlet

function GetProjectRoot($projectIns) {    
    $fullname = $projectIns.FullName
    [System.IO.Path]::GetDirectoryName($fullname)
}

function Test-CallNewPackageWithNoParameterInWebSite {
    param(
        $context
    )

    # Arrange
    $project = New-WebSite
    
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    
    # Act
    New-Package -ProjectName $project.Name  
    
    # Act
    $packagePath = Join-Path (GetProjectRoot($project)) "coolpackage.1.3.nupkg"
    
    Assert-PathExists $packagePath 
}

function Test-CallNewPackageWithNoParameterInProject {
    param(
        $context
    )

    # Arrange
    $project = New-ConsoleApplication
    
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    
    # Act
    New-Package -ProjectName $project.Name
    
    # Act
    $packagePath = Join-Path (GetProjectRoot($project)) "coolpackage.1.3.nupkg"
    
    Assert-PathExists $packagePath 
}

function Test-CallNewPackageInWebSiteSettingSpecParameter {
    param(
        $context
    )

    # Arrange
    $project = New-WebSite
    
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    
    # Act
    New-Package -ProjectName $project.Name -SpecFileName "coolpackage.nuspec"
    
    # Act
    $packagePath = Join-Path (GetProjectRoot($project)) "coolpackage.1.3.nupkg"
    
    Assert-PathExists $packagePath 
}

function Test-CallNewPackageInWebSiteSettingTargetFileParameter {
    param(
        $context
    )

    # Arrange
    $project = New-WebSite
    
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    
    # Act
    New-Package -ProjectName $project.Name -TargetFile "luan.nupkg"
    
    # Act
    $packagePath = Join-Path (GetProjectRoot($project)) "luan.nupkg"
    
    Assert-PathExists $packagePath 
}

function Test-CallNewPackageInWebApplicationSettingTargetFileParameter {
    param(
        $context
    )

    # Arrange
    $project = New-WebApplication
    
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    
    # Act
    New-Package -ProjectName $project.Name -TargetFile "dotnetjunky.nupkg"
    
    # Act
    $packagePath = Join-Path (GetProjectRoot($project)) "dotnetjunky.nupkg"
    
    Assert-PathExists $packagePath 
}

function Test-CallNewPackageWhenThereAreNoSpecFileInProject {
    param(
        $context
    )

    # Arrange
    $project = New-WebApplication
    
    # Act
    $action = { New-Package -ProjectName $project.Name -TargetFile "dotnetjunky.nupkg" }
    
    # Act    
    Assert-Throws $action "Unable to locate a .nuspec file in the specified project."
    
}

function Test-CallNewPackageWhenThereAreTwoSpecFileInProject {
    param(
        $context
    )

    # Arrange
    $project = New-WebApplication
    Add-File $project "$($context.RepositoryRoot)\coolpackage.nuspec"
    Add-File $project "$($context.RepositoryRoot)\secondpackage.nuspec"
    
    # Act
    $action = { New-Package -ProjectName $project.Name -TargetFile "dotnetjunky.nupkg" }
    
    # Act    
    Assert-Throws $action "More than one .nuspec files were found."
    
}