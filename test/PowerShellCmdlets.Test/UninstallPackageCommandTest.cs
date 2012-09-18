using System;
using System.Management.Automation;
using EnvDTE;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;

namespace NuGet.PowerShell.Commands.Test
{
    public class UninstallPackageCommandTest
    {
        [Fact]
        public void UninstallPackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var uninstallCmdlet = new UninstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => uninstallCmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void UninstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified()
        {
            // Arrange
            var id = "my-id";
            var version = new SemanticVersion("2.8");
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object) { CallBase = true };
            uninstallCmdlet.Object.Id = id;
            uninstallCmdlet.Object.Version = version;

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
        }

        [Fact]
        public void UninstallPackageCmdletPassesForceSwitchCorrectly()
        {
            // Arrange
            var id = "my-id";
            var version = new SemanticVersion("2.8");
            var forceSwitch = true;
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object) { CallBase = true };
            uninstallCmdlet.Object.Id = id;
            uninstallCmdlet.Object.Version = version;
            uninstallCmdlet.Object.Force = new SwitchParameter(forceSwitch);

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
            Assert.True(vsPackageManager.ForceRemove);
        }

        [Fact]
        public void UninstallPackageCmdletPassesRemoveDependencyCorrectly()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var uninstallCmdlet = new Mock<UninstallPackageCommand>(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object) { CallBase = true };
            uninstallCmdlet.Object.Id = "my-id";
            uninstallCmdlet.Object.Version = new SemanticVersion("2.8");
            uninstallCmdlet.Object.RemoveDependencies = new SwitchParameter(true);

            // Act
            uninstallCmdlet.Object.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
            Assert.True(vsPackageManager.RemoveDependencies);
        }

        private class MockVsPackageManager : VsPackageManager
        {
            public MockVsPackageManager()
                : base(new Mock<ISolutionManager>().Object, new Mock<IPackageRepository>().Object, new Mock<IFileSystemProvider>().Object, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object)
            {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public SemanticVersion Version { get; set; }

            public bool ForceRemove { get; set; }

            public bool RemoveDependencies { get; set; }

            public override void UninstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool forceRemove, bool removeDependencies, ILogger logger)
            {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                ForceRemove = forceRemove;
                RemoveDependencies = removeDependencies;
            }

            public override IProjectManager GetProjectManager(Project project)
            {
                return new Mock<IProjectManager>().Object;
            }
        }
    }
}