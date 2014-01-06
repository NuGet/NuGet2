using System;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class VsUninstallerTest
    {
        [Fact]
        public void UninstallPackageExecutesUninstallScript()
        {
            // Arrange
            var activeRepository = new MockPackageRepository();
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true };
            localRepository.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var project = TestUtils.GetProject("Foo");
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETCore, Version=4.5"));
            var projectManager = new ProjectManager(activeRepository, new DefaultPackagePathResolver(new MockFileSystem()), projectSystem, projectRepository);
            var package = PackageUtility.CreatePackage("A", content: new[] {"file1.txt"});
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"C:\MockFileSystem\A.1.0", "uninstall.ps1", package, project, new FrameworkName(".NETCore, Version=4.5"), NullLogger.Instance)).Returns(true).Verifiable();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                activeRepository, 
                new Mock<IFileSystemProvider>().Object, 
                new MockFileSystem(), 
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            projectManager.LocalRepository.AddPackage(package);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(activeRepository, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(packageManagerFactory.Object, activeRepository, scriptExecutor.Object);

            // Act
            localRepository.Object.AddPackage(package);
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: true);

            // Assert
            scriptExecutor.Verify();
            Assert.False(localRepository.Object.Contains(package));
        }

        [Fact]
        public void UninstallPackageDoesNotRemoveDependenciesIfFlagIsFalse()
        {
            // Arrange
            var activeRepository = new MockPackageRepository();
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true };
            localRepository.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var project = TestUtils.GetProject("Foo");
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETCore, Version=4.5"));
            var projectManager = new ProjectManager(activeRepository, new DefaultPackagePathResolver(new MockFileSystem()), projectSystem, projectRepository);
            var packageA = PackageUtility.CreatePackage("A", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B");
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"C:\MockFileSystem\A.1.0", "uninstall.ps1", packageA, project, new FrameworkName(".NETCore, Version=4.5"), NullLogger.Instance)).Returns(true).Verifiable();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                activeRepository, 
                new Mock<IFileSystemProvider>().Object, 
                new MockFileSystem(), 
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            projectManager.LocalRepository.AddPackage(packageA);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(activeRepository, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(packageManagerFactory.Object, activeRepository, scriptExecutor.Object);

            // Act
            localRepository.Object.AddPackage(packageA);
            localRepository.Object.AddPackage(packageB);
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: false);

            // Assert
            scriptExecutor.Verify();
            Assert.False(localRepository.Object.Contains(packageA));
            Assert.True(localRepository.Object.Contains(packageB));
        }

        [Fact]
        public void UninstallPackageRemovesDependenciesIfFlagIsTrue()
        {
            // Arrange
            var activeRepository = new MockPackageRepository();
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true };
            localRepository.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var project = TestUtils.GetProject("Foo");
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(activeRepository, new DefaultPackagePathResolver(new MockFileSystem()), projectSystem, projectRepository);
            var packageA = PackageUtility.CreatePackage("A", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", content: new[] {"file1.txt"});
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"C:\MockFileSystem\A.1.0", "uninstall.ps1", packageA, project, It.IsAny<FrameworkName>(), NullLogger.Instance)).Returns(true).Verifiable();
            scriptExecutor.Setup(s => s.Execute(@"C:\MockFileSystem\B.1.0", "uninstall.ps1", packageB, project, It.IsAny<FrameworkName>(), NullLogger.Instance)).Returns(true).Verifiable();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                activeRepository, 
                new Mock<IFileSystemProvider>().Object, 
                new MockFileSystem(), 
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(activeRepository, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(packageManagerFactory.Object, activeRepository, scriptExecutor.Object);

            // Act
            localRepository.Object.AddPackage(packageA);
            localRepository.Object.AddPackage(packageB);
            packageUninstaller.UninstallPackage(project, "A", removeDependencies: true);

            // Assert
            scriptExecutor.Verify();
            Assert.False(localRepository.Object.Contains(packageA));
            Assert.False(localRepository.Object.Contains(packageB));
        }

        [Fact]
        public void UninstallPackageDoesNotForceRemovesPackages()
        {
            // Arrange
            var activeRepository = new MockPackageRepository();
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true };
            localRepository.As<ISharedPackageRepository>();
            var projectRepository = new MockProjectPackageRepository(localRepository.Object);
            var project = TestUtils.GetProject("Foo");
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(activeRepository, new DefaultPackagePathResolver(new MockFileSystem()), projectSystem, projectRepository);
            var packageA = PackageUtility.CreatePackage("A", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B");
            var scriptExecutor = new Mock<IScriptExecutor>(MockBehavior.Strict);
            scriptExecutor.Setup(s => s.Execute(@"C:\MockFileSystem\A.1.0", "uninstall.ps1", packageA, project, It.IsAny<FrameworkName>(), NullLogger.Instance)).Returns(true).Verifiable();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                activeRepository, 
                new Mock<IFileSystemProvider>().Object, 
                new MockFileSystem(), 
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                null) 
                { 
                    CallBase = true 
                };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(activeRepository, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager).Verifiable();
            var packageUninstaller = new VsPackageUninstaller(packageManagerFactory.Object, activeRepository, scriptExecutor.Object);


            // Act and Assert
            localRepository.Object.AddPackage(packageA);
            localRepository.Object.AddPackage(packageB);
            ExceptionAssert.Throws<InvalidOperationException>(() => packageUninstaller.UninstallPackage(project, "B", removeDependencies: true),
                "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it.");
        }
    }
}
;