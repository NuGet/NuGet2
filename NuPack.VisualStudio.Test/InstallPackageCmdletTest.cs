using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Test;
using NuPack.VisualStudio.Cmdlets;

namespace NuPack.VisualStudio.Test {
    [TestClass]
    public class InstallPackageCmdletTest {
        [TestMethod]
        public void InstallPackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var cmdlet = new InstallPackageCmdlet(TestUtils.GetSolutionManager(isSolutionOpen: false), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void InstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var vsPackageManager = new MockVsPackageManager();
            var cmdlet = new InstallPackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            cmdlet.Id = id;
            cmdlet.Version = version;

            // Act
            cmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
        }

        [TestMethod]
        public void InstallPackageCmdletPassesIgnoreDependencySwitchCorrectly() {
            // Arrange
            var id = "my-id";
            var version = new Version("2.8");
            var ignoreDependencies = true;
            var vsPackageManager = new MockVsPackageManager();
            var cmdlet = new InstallPackageCmdlet(TestUtils.GetSolutionManager(), new Mock<IPackageRepositoryFactory>().Object, TestUtils.GetDTE(), vsPackageManager);
            cmdlet.Id = id;
            cmdlet.Version = version;
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: ignoreDependencies);

            // Act
            cmdlet.GetResults();

            // Assert
            Assert.AreEqual(vsPackageManager.PackageId, id);
            Assert.AreEqual(vsPackageManager.Version, version);
            Assert.AreEqual(vsPackageManager.IgnoreDependencies, ignoreDependencies);
        }

        private class MockVsPackageManager : VsPackageManager {

            public MockVsPackageManager()
                : base(TestUtils.GetSolutionManager(), new Mock<IPackageRepository>().Object,
                new Mock<IFileSystem>().Object, new Mock<IPackageRepository>().Object) {
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
