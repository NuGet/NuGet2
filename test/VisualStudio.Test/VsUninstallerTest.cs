using System;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using NuGet.Resolver;
    using PackageUtility = NuGet.Test.PackageUtility;

    public class VsUninstallerTest
    {
        private void InstallPackage(string id, MockVsPackageManager2 packageManager)
        {
            var projectManager = packageManager.GetProjectManager(packageManager.SolutionManager.GetProject("default"));
            
            // Resolve the package to install
            IPackage package = PackageRepositoryHelper.ResolvePackage(
                packageManager.SourceRepository,
                packageManager.LocalRepository,
                id,
                version: null,
                allowPrereleaseVersions: false);

            // Resolve operations
            var resolver = new ActionResolver();
            resolver.AddOperation(NuGet.PackageAction.Install, package, projectManager);
            var actions = resolver.ResolveActions();

            var executor = new ActionExecutor();
            executor.Execute(actions);
        }

        [Fact]
        public void UninstallPackageExecutesUninstallScript()
        {
            // Arrange            
            var packageA = PackageUtility.CreatePackage("A", content: new[] {"file1.txt"});
            var packageRepository = new MockPackageRepository { packageA, };
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage("A", packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var project = packageManager.SolutionManager.GetProject("default");
            scriptExecutor.Setup(s => s.Execute(@"c:\solution\A.1.0", "uninstall.ps1", packageA, project, 
                new FrameworkName(".NETFramework,Version=v4.0.0.0"), NullLogger.Instance)).Returns(true).Verifiable();

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager);

            var packageUninstaller = new VsPackageUninstaller(
                packageManagerFactory.Object, 
                packageManager.LocalRepository,
                scriptExecutor.Object);

            // Act
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: true);

            // Assert
            scriptExecutor.Verify();
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(0, installedPackages.Count);
        }

        [Fact]
        public void UninstallPackageDoesNotRemoveDependenciesIfFlagIsFalse()
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

            var project = packageManager.SolutionManager.GetProject("default");
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager);
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"c:\solution\A.1.0", "uninstall.ps1", packageA, project,
                new FrameworkName(".NETFramework,Version=v4.0.0.0"), NullLogger.Instance)).Returns(true).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(
                packageManagerFactory.Object,
                packageManager.LocalRepository,
                scriptExecutor.Object);

            // Act            
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: false);

            // Assert: packageA is uninstalled, while packageB is not
            scriptExecutor.Verify();
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageB }, installedPackages, PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void UninstallPackageRemovesDependenciesIfFlagIsTrue()
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

            var project = packageManager.SolutionManager.GetProject("default");
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager);
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"c:\solution\A.1.0", "uninstall.ps1", packageA, project,
                new FrameworkName(".NETFramework,Version=v4.0.0.0"), NullLogger.Instance)).Returns(true).Verifiable();
            scriptExecutor.Setup(s => s.Execute(@"c:\solution\B.1.0", "uninstall.ps1", packageB, project, It.IsAny<FrameworkName>(), NullLogger.Instance)).Returns(true).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(
                packageManagerFactory.Object,
                packageManager.LocalRepository,
                scriptExecutor.Object);

            // Act            
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: true);

            // Assert: both packageA and packageB are uninstalled
            scriptExecutor.Verify();
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(0, installedPackages.Count);
        }

        [Fact]
        public void UninstallPackageDoesNotForceRemovesPackages()
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

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager);
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            var packageUninstaller = new VsPackageUninstaller(
                packageManagerFactory.Object,
                packageManager.LocalRepository, 
                scriptExecutor.Object);

            
            // Act and Assert
            var project = packageManager.SolutionManager.GetProject("default");
            ExceptionAssert.Throws<InvalidOperationException>(() => packageUninstaller.UninstallPackage(project, "B", removeDependencies: true),
                "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        }
    }
}