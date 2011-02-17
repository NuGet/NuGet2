function Test-PackageManagerServicesAreAvailableThroughMEF {
    # Arrange
    $cm = Get-VsComponentModel

    # Act
    $installer = $cm.GetService([NuGet.VisualStudio.IVsPackageInstaller])

    # Assert
    Assert-NotNull $installer
}