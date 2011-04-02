using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.PowerShell.Commands.Test {
    [TestClass]
    public class InstallPackageCommandTest {
        [TestMethod]
        public void InstallPackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void InstallPackageCmdletUsesPackageManangerWithSourceIfSpecified() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var vsPackageManager = new MockVsPackageManager();
            var sourceVsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            packageManagerFactory.Setup(m => m.CreatePackageManager("somesource")).Returns(sourceVsPackageManager);
            var cmdlet = new Mock<InstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null) { CallBase = true };
            cmdlet.Object.Source = "somesource";
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreSame(sourceVsPackageManager, cmdlet.Object.PackageManager);
        }

        [TestMethod]
        public void InstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var cmdlet = new Mock<InstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
        }

        [TestMethod]
        public void InstallPackageCmdletPassesIgnoreDependencySwitchCorrectly() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var cmdlet = new Mock<InstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");
            cmdlet.Object.IgnoreDependencies = new SwitchParameter(true);

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
            Assert.IsTrue(vsPackageManager.IgnoreDependencies);
        }

        [TestMethod]
        public void InstallPackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddress() {
            // Arrange
            string source = "http://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<string>())).Returns(vsPackageManager);
            var cmdlet = new Mock<InstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, productUpdateService.Object) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");
            cmdlet.Object.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Object.Source = source;

            // Act
            cmdlet.Object.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [TestMethod]
        public void InstallPackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddress() {
            // Arrange
            string source = "ftp://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<string>())).Returns(vsPackageManager);
            var cmdlet = new Mock<InstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, productUpdateService.Object) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");
            cmdlet.Object.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Object.Source = source;

            // Act
            cmdlet.Object.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        private class MockVsPackageManager : VsPackageManager {
            public MockVsPackageManager()
                : this(new Mock<IPackageRepository>().Object) {
            }

            public MockVsPackageManager(IPackageRepository sourceRepository)
                : base(new Mock<ISolutionManager>().Object, sourceRepository, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IRecentPackageRepository>().Object) {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public Version Version { get; set; }

            public bool IgnoreDependencies { get; set; }

            public override void InstallPackage(IProjectManager projectManager, string packageId, Version version, bool ignoreDependencies, ILogger logger) {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                IgnoreDependencies = ignoreDependencies;
            }

            public override IProjectManager GetProjectManager(Project project) {
                return new Mock<IProjectManager>().Object;
            }
        }

    }
}
