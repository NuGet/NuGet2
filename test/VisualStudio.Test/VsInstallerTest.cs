using System;
using System.Linq;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsPackageInstallerTest
    {
        [Fact]
        public void InstallPackageConvertsVersionToSemanticVersion()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object, new Mock<IPackageRepository>().Object) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, packageRepositoryFactory.Object);

            // Act
            installer.InstallPackage(@"x:\test", project, "foo", new Version("1.0"), ignoreDependencies: false);

            // Assert
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Init, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Install, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
            packageRepositoryFactory.Verify();
        }

        [Fact]
        public void InstallPackageRunsInitAndInstallScripts()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object, new Mock<IPackageRepository>().Object) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false);

            // Assert
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Init, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Install, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
        }

        [Fact]
        public void InstallPackageDoesNotUseFallbackRepository()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object, new Mock<IPackageRepository>().Object) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false)).Returns(packageManager.Object);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Throws(new Exception("A"));
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true, It.IsAny<bool>())).Throws(new Exception("B"));
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false);

            // Assert
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false), Times.Once());
            packageManagerFactory.Verify(m => m.CreatePackageManager(), Times.Never());
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true, It.IsAny<bool>()), Times.Never());
        }

        [Fact]
        public void InstallPackageDoesNotAddToRecentRepository()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object, new Mock<IPackageRepository>().Object) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false)).Returns(packageManager.Object);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Throws(new Exception("A"));
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true, It.IsAny<bool>())).Throws(new Exception("B"));
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false);

            // Assert
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false, false), Times.Once());
            packageManagerFactory.Verify(m => m.CreatePackageManager(), Times.Never());
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), It.IsAny<bool>(), true), Times.Never());
        }
    }
}
