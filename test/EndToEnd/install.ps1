function Test-SinglePackageInstallIntoSingleProject {
    # Arrange
    $p = New-ConsoleApplication
    
    # Act
    Install-Package FakeItEasy -Project $p.Name
    
    # Assert
    Assert-References $p @("Castle.Core", "FakeItEasy")
    Assert-PackagesConfig $p
    Assert-SolutionPackageExists FakeItEasy
    Assert-SolutionPackageExists Castle.Core
}