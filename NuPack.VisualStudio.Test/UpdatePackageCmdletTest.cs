using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Test;
using NuPack.VisualStudio.Cmdlets;

namespace NuPack.VisualStudio.Test {
    [TestClass]
    public class UpdatePackageCmdletTest {
        [TestMethod]
        public void UpdatePackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var cmdlet = new UpdatePackageCmdlet(TestUtils.GetSolutionManager(isSolutionOpen: false), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var vsPackageManager = new MockVsPackageManager();
            var cmdlet = new UpdatePackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            cmdlet.Id = id;
            cmdlet.Version = version;

            // Act
            cmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesIgnoreDependencySwitchCorrectly() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var updateDependencies = true;
            var vsPackageManager = new MockVsPackageManager();
            var cmdlet = new UpdatePackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            cmdlet.Id = id;
            cmdlet.Version = version;
            cmdlet.UpdateDependencies = new SwitchParameter(isPresent: updateDependencies);

            // Act
            cmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
            Assert.AreEqual(vsPackageManager.UpdateDependencies, updateDependencies);
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
