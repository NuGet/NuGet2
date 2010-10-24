using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Test;
using NuPack.VisualStudio.Cmdlets;

namespace NuPack.VisualStudio.Test {
    [TestClass]
    public class UninstallPackageCmdletTest {
        [TestMethod]
        public void UninstallPackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var uninstallCmdlet = new UninstallPackageCmdlet(TestUtils.GetSolutionManager(isSolutionOpen: false), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), null);

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
            var uninstallCmdlet = new UninstallPackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            uninstallCmdlet.Id = id;
            uninstallCmdlet.Version = version;

            // Act
            uninstallCmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
        }

        [TestMethod]
        public void UninstallPackageCmdletPassesForceSwitchCorrectly() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var forceSwitch = true;
            var vsPackageManager = new MockVsPackageManager();
            var uninstallCmdlet = new UninstallPackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            uninstallCmdlet.Id = id;
            uninstallCmdlet.Version = version;
            uninstallCmdlet.Force = new SwitchParameter(forceSwitch);

            // Act
            uninstallCmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
            Assert.AreEqual(vsPackageManager.ForceRemove, forceSwitch);
        }

        [TestMethod]
        public void UninstallPackageCmdletPassesRemoveDependencyCorrectly() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var removeDependencies = true;
            var vsPackageManager = new MockVsPackageManager();
            var uninstallCmdlet = new UninstallPackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            uninstallCmdlet.Id = id;
            uninstallCmdlet.Version = version;
            uninstallCmdlet.RemoveDependencies = new SwitchParameter(removeDependencies);

            // Act
            uninstallCmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
            Assert.AreEqual(vsPackageManager.RemoveDependencies, removeDependencies);
        }

        private class MockVsPackageManager : VsPackageManager {

            public MockVsPackageManager() 
                : base(TestUtils.GetSolutionManager(), new Mock<IPackageRepository>().Object,
                new Mock<IFileSystem>().Object, new Mock<IPackageRepository>().Object) {
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
