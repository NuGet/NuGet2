using System;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class VsInstallerTest {
        [TestMethod]
        public void InstallPackageRunsInitAndInstallScripts() {
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
            var packageManager = new Mock<VsPackageManager>(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository, new Mock<IRecentPackageRepository>().Object) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>())).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new Version("1.0"), ignoreDependencies: false);

            // Assert
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Init, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Install, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<ILogger>()), Times.Once());
        }

        // This repository better simulates what happens when we're running the package manager in vs
        private class MockProjectPackageRepository : MockPackageRepository {
            private readonly IPackageRepository _parent;
            public MockProjectPackageRepository(IPackageRepository parent) {
                _parent = parent;
            }
            public override IQueryable<IPackage> GetPackages() {
                return base.GetPackages().Where(p => _parent.Exists(p));
            }
        }
    }
}
