using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio.Cmdlets;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class UpdatePackageCmdletTest {
        [TestMethod]
        public void UpdatePackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new UpdatePackageCmdlet(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void UpdatePackageCmdletUsesPackageManangerWithSourceIfSpecified() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var vsPackageManager = new MockVsPackageManager();
            var sourceVsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            packageManagerFactory.Setup(m => m.CreatePackageManager("somesource")).Returns(sourceVsPackageManager);
            var cmdlet = new Mock<UpdatePackageCmdlet>(TestUtils.GetSolutionManager(), packageManagerFactory.Object) { CallBase = true };
            cmdlet.Object.Source = "somesource";
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreSame(sourceVsPackageManager, cmdlet.Object.PackageManager);
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var cmdlet = new Mock<UpdatePackageCmdlet>(TestUtils.GetSolutionManager(), packageManagerFactory.Object) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesIgnoreDependencySwitchCorrectly() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var cmdlet = new Mock<UpdatePackageCmdlet>(TestUtils.GetSolutionManager(), packageManagerFactory.Object) { CallBase = true };
            cmdlet.Object.Id = "my-id";
            cmdlet.Object.Version = new Version("2.8");
            cmdlet.Object.UpdateDependencies = new SwitchParameter(isPresent: true);

            // Act
            cmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"),  vsPackageManager.Version);
            Assert.IsTrue(vsPackageManager.UpdateDependencies);
        }

        private class MockVsPackageManager : VsPackageManager {

            public MockVsPackageManager()
                : base(TestUtils.GetSolutionManager(), new Mock<IPackageRepository>().Object,
                new Mock<IFileSystem>().Object, new Mock<IPackageRepository>().Object) {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public Version Version { get; set; }

            public bool UpdateDependencies { get; set; }

            public override void UpdatePackage(IProjectManager projectManager, string packageId, Version version, bool updateDependencies, ILogger logger) {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                UpdateDependencies = updateDependencies;
            }

            public override IProjectManager GetProjectManager(Project project) {
                return new Mock<IProjectManager>().Object;
            }
        }

    }
}
