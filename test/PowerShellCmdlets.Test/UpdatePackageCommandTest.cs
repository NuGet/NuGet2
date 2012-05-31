using System;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;
using Xunit.Extensions;

namespace NuGet.PowerShell.Commands.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;
    public class UpdatePackageCommandTest
    {
        [Fact]
        public void UpdatePackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null, null, null, new Mock<IVsCommonOperations>().Object);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void UpdatePackageCmdletUsesPackageManangerWithSourceIfSpecified()
        {
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Source = "somesource";
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Same(sourceVsPackageManager, cmdlet.PackageManager);
        }

        [Fact]
        public void UpdatePackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
        }

        [Fact]
        public void UpdatePackageCmdletSpecifiesUpdateOperationDuringExecution()
        {
            // Arrange
            var mockPackageRepository = new MockPackageRepository();
            var vsPackageManager = new MockVsPackageManager(mockPackageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal(RepositoryOperationNames.Update, mockPackageRepository.LastOperation);
        }

        [Fact]
        public void UpdatePackageCmdletPassesIgnoreDependencySwitchCorrectly()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
            Assert.True(vsPackageManager.UpdateDependencies);
        }

        [Fact]
        public void UpdatePackageCmdletPassesIgnoreDependencySwitchCorrectlyWhenPresent()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
            Assert.False(vsPackageManager.UpdateDependencies);
        }

        [Fact]
        public void UpdatePackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddress()
        {
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = source;
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void UpdatePackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddressAndSourceIsSpecified()
        {
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = "bing";
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void UpdatePackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddress()
        {
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = source;
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0-alpha")]
        [InlineData("1.0.0-beta", "2.0.0")]
        [InlineData("1.0.0-beta", "1.0.1-beta")]
        [InlineData("1.0.0", "1.0.1")]
        public void UpdatePackageDoNotUpdateToUnlistedPackageWithPrerelease(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1, dependencies: new[] { new PackageDependency("B") });
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(new [] { packageA1 }.AsQueryable());

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, recentPackageRepository.Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }

        [Theory]
        [InlineData("1.0.0", "1.0.1-alpha")]
        [InlineData("1.0.0-beta", "1.0.9")]
        [InlineData("1.0.0-beta", "1.0.1-beta")]
        [InlineData("1.0.0", "1.0.1")]
        public void SafeUpdatePackageDoNotUpdateToUnlistedPackageWithPrerelease(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1, dependencies: new[] { new PackageDependency("B") });
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(new[] { packageA1 }.AsQueryable());

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, recentPackageRepository.Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Safe = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("1.0.0", "1.0.1")]
        public void UpdatePackageDoNotUpdateToUnlistedPackage(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1, dependencies: new[] { new PackageDependency("B") });
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(new[] { packageA1 }.AsQueryable());

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, recentPackageRepository.Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }

        [Theory]
        [InlineData("1.0.0", "1.0.0.2")]
        [InlineData("1.0.0", "1.0.1.3")]
        public void SafeUpdatePackageDoNotUpdateToUnlistedPackage(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1, dependencies: new[] { new PackageDependency("B") });
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(new[] { packageA1 }.AsQueryable());

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, recentPackageRepository.Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "A";
            cmdlet.Safe = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("1.0.0", "1.0.1")]
        [InlineData("1.0.0", "2.0.0-alpha")]
        [InlineData("1.0.0-beta", "2.0.0")]
        [InlineData("1.0.0-beta", "1.0.1-beta")]
        public void UpdatePackageUpdateToUnlistedPackageIfVersionIsSet(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1, dependencies: new[] { new PackageDependency("B") });
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(new[] { packageA1 }.AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA2)).Verifiable();

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, recentPackageRepository.Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion(versionA2);
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
        }

        private static IVsPackageSourceProvider GetPackageSourceProvider(params PackageSource[] sources)
        {
            var sourceProvider = new Mock<IVsPackageSourceProvider>();
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            return sourceProvider.Object;
        }

        private class MockVsPackageManager : VsPackageManager
        {
            public MockVsPackageManager()
                : this(new Mock<IPackageRepository>().Object)
            {
            }

            public MockVsPackageManager(IPackageRepository sourceRepository)
                : base(new Mock<ISolutionManager>().Object, sourceRepository, new Mock<IFileSystemProvider>().Object, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object)
            {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public SemanticVersion Version { get; set; }

            public bool UpdateDependencies { get; set; }

            public override void UpdatePackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool updateDependencies, bool allowPreReleaseVersions, ILogger logger)
            {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                UpdateDependencies = updateDependencies;
            }

            public override IProjectManager GetProjectManager(Project project)
            {
                return new Mock<IProjectManager>().Object;
            }
        }
    }
}