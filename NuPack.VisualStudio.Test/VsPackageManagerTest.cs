using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.Test.VisualStudio {
    [TestClass]
    public class VsPackageManagerTest {
        [TestMethod]
        public void InstallPackageInstallsIntoProjectAndPackageManager() {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, projectSystem, localRepository);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new Version("1.0"), ignoreDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
            Assert.IsTrue(projectManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void InstallPackgeWithNullProjectManagerOnlyInstallsIntoPackageManager() {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, projectSystem, localRepository);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(null, "foo", new Version("1.0"), ignoreDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void UninstallPackageDoesNotRemovePackageIfPackageIsReferenced() {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("foo", It.IsAny<Version>())).Returns(true);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.Object.AddPackage(package);
            sourceRepository.AddPackage(package);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository.Object);

            // Act
            packageManager.UninstallPackage(null, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void UninstallPackageRemovesPackageIfPackageIsNotReferenced() {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("foo", It.IsAny<Version>())).Returns(false);
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            localRepository.Object.AddPackage(package);
            sourceRepository.AddPackage(package);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository.Object);

            // Act
            packageManager.UninstallPackage(null, "foo", version: null, forceRemove: false, removeDependencies: false, logger: NullLogger.Instance);

            // Assert
            Assert.IsFalse(packageManager.LocalRepository.Exists(package));
        }

        [TestMethod]
        public void UpdatePackageRemovesPackageIfPackageIsNotReferenced() {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new Version("1.0"))).Returns(false);
            var projectRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", new[] { "hello" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", new[] { "hello" });
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(A20);
            projectRepository.Add(A10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, logger: NullLogger.Instance);

            // Assert
            Assert.IsFalse(packageManager.LocalRepository.Exists(A10));
        }

        [TestMethod]
        public void UpdatePackageDoesNotRemovesPackageIfPackageIsReferenced() {
            // Arrange            
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            localRepository.Setup(m => m.IsReferenced("A", new Version("1.0"))).Returns(true);
            var projectRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var A10 = PackageUtility.CreatePackage("A", "1.0", new[] { "hello" });
            var A20 = PackageUtility.CreatePackage("A", "2.0", new[] { "hello" });
            localRepository.Object.AddPackage(A10);
            localRepository.Object.AddPackage(A20);
            sourceRepository.AddPackage(A20);
            sourceRepository.AddPackage(A20);
            projectRepository.Add(A10);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, fileSystem, localRepository.Object);
            var projectManager = new ProjectManager(localRepository.Object, pathResolver, new MockProjectSystem(), projectRepository);

            // Act
            packageManager.UpdatePackage(projectManager, "A", version: null, updateDependencies: true, logger: NullLogger.Instance);

            // Assert
            Assert.IsTrue(packageManager.LocalRepository.Exists(A10));
        }
    }
}
