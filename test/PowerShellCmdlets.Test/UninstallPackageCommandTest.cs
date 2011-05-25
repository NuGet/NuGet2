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
    public class UninstallPackageCommandTest {
        [TestMethod]
        public void UninstallPackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var uninstallCmdlet = new UninstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => uninstallCmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void UninstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null) { CallBase = true };
            uninstallCmdlet.Object.Id = id;
            uninstallCmdlet.Object.Version = version;

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
        }

        [TestMethod]
        public void UninstallPackageCmdletPassesForceSwitchCorrectly() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var forceSwitch = true;
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null) { CallBase = true };
            uninstallCmdlet.Object.Id = id;
            uninstallCmdlet.Object.Version = version;
            uninstallCmdlet.Object.Force = new SwitchParameter(forceSwitch);

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
            Assert.IsTrue(vsPackageManager.ForceRemove);
        }

        [TestMethod]
        public void UninstallPackageCmdletPassesRemoveDependencyCorrectly() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null) { CallBase = true };
            uninstallCmdlet.Object.Id = "my-id";
            uninstallCmdlet.Object.Version = new Version("2.8");
            uninstallCmdlet.Object.RemoveDependencies = new SwitchParameter(true);

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
            Assert.IsTrue(vsPackageManager.RemoveDependencies);
        }

        private class MockVsPackageManager : VsPackageManager {
            public MockVsPackageManager()
                : base(new Mock<ISolutionManager>().Object, new Mock<IPackageRepository>().Object, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IRecentPackageRepository>().Object) {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public Version Version { get; set; }

            public bool ForceRemove { get; set; }

            public bool RemoveDependencies { get; set; }

            public override void UninstallPackage(IProjectManager projectManager, string packageId, Version version, bool forceRemove, bool removeDependencies, ILogger logger) {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                ForceRemove = forceRemove;
                RemoveDependencies = removeDependencies;
            }

            public override IProjectManager GetProjectManager(Project project) {
                return new Mock<IProjectManager>().Object;
            }
        }
    }
}
