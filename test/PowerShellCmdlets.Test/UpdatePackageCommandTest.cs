using System;
using System.Management.Automation;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.PowerShell.Commands.Test {
    [TestClass]
    public class UpdatePackageCommandTest {
        [TestMethod]
        public void UpdatePackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null, null, null);

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
            var mockPackageRepository = new MockPackageRepository();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            packageManagerFactory.Setup(m => m.CreatePackageManager(mockPackageRepository, true)).Returns(sourceVsPackageManager);
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null);
            cmdlet.Source = "somesource";
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.AreSame(sourceVsPackageManager, cmdlet.PackageManager);
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

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
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
            Assert.IsTrue(vsPackageManager.UpdateDependencies);
        }

        [TestMethod]
        public void UpdatePackageCmdletPassesIgnoreDependencySwitchCorrectlyWhenPresent() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.AreEqual("my-id", vsPackageManager.PackageId);
            Assert.AreEqual(new Version("2.8"), vsPackageManager.Version);
            Assert.IsFalse(vsPackageManager.UpdateDependencies);
        }

        [TestMethod]
        public void UpdatePackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddress() {
            // Arrange
            string source = "http://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(c => c.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = source;
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [TestMethod]
        public void UpdatePackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddressAndSourceIsSpecified() {
            // Arrange
            string source = "http://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(c => c.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source, "bing"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = "bing";
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [TestMethod]
        public void UpdatePackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddress() {
            // Arrange
            string source = "ftp://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source, "bing"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new Version("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = source;
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }



        private static IVsPackageSourceProvider GetPackageSourceProvider(params PackageSource[] sources) {
            var sourceProvider = new Mock<IVsPackageSourceProvider>();
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            return sourceProvider.Object;
        }

        private class MockVsPackageManager : VsPackageManager {
            public MockVsPackageManager()
                : this(new Mock<IPackageRepository>().Object) {
            }

            public MockVsPackageManager(IPackageRepository sourceRepository)
                : base(new Mock<ISolutionManager>().Object, sourceRepository, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object) {
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
