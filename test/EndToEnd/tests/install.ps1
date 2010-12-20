function Test-SinglePackageInstallIntoSingleProject {
    # Arrange
    $project = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $project.Name
    
    # Assert
    Assert-Reference $project Castle.Core
    Assert-Reference $project FakeItEasy   
    Assert-Package $project FakeItEasy
    Assert-Package $project Castle.Core
    Assert-SolutionPackage FakeItEasy
    Assert-SolutionPackage Castle.Core
}

function Test-WebsiteSimpleInstall {
    # Arrange
    $p = New-WebSite
    
    # Act
    Install-Package AntiXSS -Project $p.Name
    
    # Assert
    Assert-Package $p AntiXSS
    Assert-SolutionPackage AntiXSS
    Assert-Reference $p AntiXSSLibrary 4.0.0.0
    Assert-Reference $p HtmlSanitizationLibrary 4.0.0.0
}