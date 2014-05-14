using System;
using System.Linq;
using System.Management.Automation;
using Moq;
using NuGet.Resolver;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;

namespace NuGet.PowerShell.Commands.Test
{
    public class UninstallPackageCommandTest
    {
        private void InstallPackage(string id, MockVsPackageManager2 packageManager)
        {
            var resolver = new OperationResolver(packageManager);
            var userOperationExecutor = new OperationExecutor();
            var projectManager = packageManager.GetProjectManager(packageManager.SolutionManager.GetProject("default"));

            // Resolve the package to install
            IPackage package = PackageRepositoryHelper.ResolvePackage(
                packageManager.SourceRepository,
                packageManager.LocalRepository,
                id,
                version: null,
                allowPrereleaseVersions: false);

            // Resolve operations
            var projectOps = resolver.ResolveProjectOperations(
                UserOperation.Install,
                package, 
                new VirtualProjectManager(projectManager));
            var operations = resolver.ResolveFinalOperations(projectOps);

            var operationExecutor = new OperationExecutor();
            operationExecutor.Execute(operations);
        }

        [Fact]
        public void UninstallPackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var uninstallCmdlet = new UninstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => uninstallCmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void UninstallPackageCmdletWhenIdAndVersionAreSpecified()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageRepository = new MockPackageRepository { packageA };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage("A", packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);
            var cmdlet = new UninstallPackageCommand(
                packageManager.SolutionManager,
                packageManagerFactory.Object,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("1.1");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "Unable to find package 'A'.");
        }

        // Test that a dependency package can be uninstalled if -Force is specified.
        [Fact]
        public void UninstallPackageCmdletPassesForceSwitchCorrectly()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage(
                "A", "1.0",
                dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0");
            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            InstallPackage("A", packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA, packageB }, installedPackages, PackageEqualityComparer.IdAndVersion);

            // Act
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);
            var cmdlet = new UninstallPackageCommand(
                packageManager.SolutionManager,
                packageManagerFactory.Object,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "B";

            // Assert: packageB cannot be uninstalled without -Force
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.Execute(),
                "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");

            // Assert: packageB can be uninstalled with -Force
            cmdlet.Force = new SwitchParameter(true);
            cmdlet.Execute();            
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void UninstallPackageCmdletWithoutRemoveDependencySwitch()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage(
                "A", "1.0",
                dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0");
            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            InstallPackage("A", packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA, packageB }, installedPackages, PackageEqualityComparer.IdAndVersion);

            // Act
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);
            var cmdlet = new UninstallPackageCommand(
                packageManager.SolutionManager,
                packageManagerFactory.Object,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert: only packageA is uninstalled
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageB, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void UninstallPackageCmdletWithRemoveDependencySwitch()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage(
                "A", "1.0",
                dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0");
            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            InstallPackage("A", packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA, packageB }, installedPackages, PackageEqualityComparer.IdAndVersion);

            // Act
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);
            var cmdlet = new UninstallPackageCommand(
                packageManager.SolutionManager,
                packageManagerFactory.Object,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.RemoveDependencies = new SwitchParameter(true);
            cmdlet.Execute();

            // Assert: both packageA & packageB are uninstalled
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(0, installedPackages.Count);
        }
    }
}