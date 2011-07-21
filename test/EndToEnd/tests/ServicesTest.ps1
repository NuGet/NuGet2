function Test-PackageManagerServicesAreAvailableThroughMEF {
    # Arrange
    $cm = Get-VsComponentModel

    # Act
    $installer = $cm.GetService([NuGet.VisualStudio.IVsPackageInstaller])
    $installerServices = $cm.GetService([NuGet.VisualStudio.IVsPackageInstallerServices])
    $installerEvents = $cm.GetService([NuGet.VisualStudio.IVsPackageInstallerEvents])

    # Assert
    Assert-NotNull $installer
    Assert-NotNull $installerServices
    Assert-NotNull $installerEvents
}

function Test-VsPackageInstallerServices {
    param(
        $context
    )

    # Arrange
    $p = New-WebApplication
    $cm = Get-VsComponentModel
    $installerServices = $cm.GetService([NuGet.VisualStudio.IVsPackageInstallerServices])
    
    # Act
    $p | Install-Package jquery -Version 1.5 -Source $context.RepositoryRoot
    $packages = @($installerServices.GetInstalledPackages())

    # Assert
    Assert-NotNull $packages
    Assert-AreEqual 1 $packages.Count
    Assert-AreEqual jQuery $packages[0].Id
}


function Test-VsPackageInstallerEvents {
    param(
        $context
    )

    try {
        # Arrange
        $p = New-WebApplication
        $cm = Get-VsComponentModel
        $installerEvents = $cm.GetService([NuGet.VisualStudio.IVsPackageInstallerEvents])
    
        $installing = 0
        $installed = 0
        $uninstalling = 0
        $uninstalled = 0

        $installingHandler = {
            $installing++
        }

        $installerEvents.add_PackageInstalling($installingHandler)

        $installedHandler = {
            $installed++
        }

        $installerEvents.add_PackageInstalled($installedHandler)

        $uninstallingHandler = {
            $uninstalling++
        }

        $installerEvents.add_PackageUninstalling($uninstallingHandler)

        $uninstalledHandler = {
            $uninstalled++
        }

        $installerEvents.add_PackageUninstalled($uninstalledHandler)

        # Act
        $p | Install-Package jquery -Version 1.5 -Source $context.RepositoryRoot
        $p | Uninstall-Package jquery
        
        # Assert
        Assert-AreEqual 1 $installing
        Assert-AreEqual 1 $installed
        Assert-AreEqual 1 $uninstalling
        Assert-AreEqual 1 $uninstalled
    }
    finally {
        $installerEvents.remove_PackageInstalling($installingHandler)
        $installerEvents.remove_PackageInstalled($installedHandler)
        $installerEvents.remove_PackageUninstalling($uninstallingHandler)
        $installerEvents.remove_PackageUninstalled($uninstalledHandler)
    }
}


