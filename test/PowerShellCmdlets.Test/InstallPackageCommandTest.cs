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

namespace NuGet.PowerShell.Commands.Test {
    using PackageUtility = NuGet.Test.PackageUtility;

    public class InstallPackageCommandTest {
        [Fact]
        public void InstallPackageCmdletThrowsWhenSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null, null, null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void InstallPackageCmdletUsesPackageManangerWithSourceIfSpecified() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var vsPackageManager = new MockVsPackageManager();
            var sourceVsPackageManager = new MockVsPackageManager();
            var mockPackageRepository = new MockPackageRepository();
            var sourceProvider = GetPackageSourceProvider(new PackageSource("somesource"));
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.Is<string>(s => s == "somesource"))).Returns(mockPackageRepository);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Returns(sourceVsPackageManager);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null);
            cmdlet.Source = "somesource";
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Same(sourceVsPackageManager, cmdlet.PackageManager);
        }

        [Fact]
        public void InstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null, null, null);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
        }

        [Fact]
        public void InstallPackageCmdletPassesIgnoreDependencySwitchCorrectly() {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null, null, null);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(true);

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
            Assert.True(vsPackageManager.IgnoreDependencies);
        }

        [Fact]
        public void InstallPackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddress() {
            // Arrange
            string source = "http://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source));
            packageRepositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Source = source;

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void InstallPackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddressAndSourceNameIsSpecified() {
            // Arrange
            string source = "http://bing.com";
            string sourceName = "bing";
            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source, sourceName));
            packageRepositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);

            packageManagerFactory.Setup(m => m.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Source = sourceName;

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void InstallPackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddress() {
            // Arrange
            string source = "ftp://bing.com";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source));
            packageRepositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Source = source;

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void InstallPackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddressAndSourceNameIsSpecified() {
            // Arrange
            string source = "ftp://bing.com";
            string sourceName = "BING";

            var productUpdateService = new Mock<IProductUpdateService>();
            var sourceRepository = new Mock<IPackageRepository>();
            sourceRepository.Setup(p => p.Source).Returns(source);
            var vsPackageManager = new MockVsPackageManager(sourceRepository.Object);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(sourceRepository.Object, true)).Returns(vsPackageManager);
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = GetPackageSourceProvider(new PackageSource(source, sourceName));
            packageRepositoryFactory.Setup(c => c.CreateRepository(source)).Returns(sourceRepository.Object);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Source = sourceName;

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void InstallPackageCmdletCreatesFallbackRepository() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            IPackageRepository repoA = new MockPackageRepository(), repoB = new MockPackageRepository();
            var package = NuGet.Test.PackageUtility.CreatePackage("P1", dependencies: new[] { new PackageDependency("P2") });
            repoA.AddPackage(package);
            repoB.AddPackage(NuGet.Test.PackageUtility.CreatePackage("P2"));
            var sharedRepo = new Mock<ISharedPackageRepository>();
            var recentRepo = new Mock<IRecentPackageRepository>();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository("A")).Returns(repoA);
            repositoryFactory.Setup(c => c.CreateRepository("B")).Returns(repoB);
            var sourceProvider = GetPackageSourceProvider(new PackageSource("A"), new PackageSource("B"));
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(c => c.RepositoryPath).Returns(String.Empty);

            var solutionManager = new Mock<ISolutionManager>();
            var packageManagerFactory = new VsPackageManagerFactory(solutionManager.Object, repositoryFactory.Object, sourceProvider, fileSystemProvider.Object, repositorySettings.Object, null, new Mock<VsPackageInstallerEvents>().Object, new MockPackageRepository());

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object);
            cmdlet.Id = "P1";
            cmdlet.Source = "A";

            // Act
            cmdlet.Execute();

            // Assert
            // If we've come this far, P1 is successfully installed.
            Assert.True(true);
        }

        [Fact]
        public void InstallPackageCmdletCreatesPackageManagerWithFallbackFlagSet() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var fallbackRepo = new Mock<IVsPackageManager>();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(c => c.CreatePackageManager()).Returns(fallbackRepo.Object).Verifiable();
            packageManagerFactory.Setup(c => c.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Throws(new Exception());
            var repoA = new MockPackageRepository();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository("A")).Returns(repoA);

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, GetPackageSourceProvider(new PackageSource("A")), null, productUpdateService.Object);
            cmdlet.Id = "P1";
            cmdlet.Source = "A";

            // Act
            cmdlet.Execute();

            // Assert
            // If we've come this far, P1 is successfully installed.
            Assert.True(true);
        }

        [Fact]
        public void InstallPackageCmdletDoesNotInstallPrereleasePackageIfFlagIsNotPresent() {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            var packageRepository = new MockPackageRepository { PackageUtility.CreatePackage("A", "1.0.0a") };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new MockFileSystem(), sharedRepository.Object, 
                recentPackageRepository.Object, null);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null, null, null);
            cmdlet.Id = "A";
            
            
            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.Execute(), "Unable to find package 'A'.");
        }

        [Fact]
        public void InstallPackageCmdletInstallPrereleasePackageIfFlagIsPresent() {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0a");
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            var packageRepository = new MockPackageRepository { packageA };
            var recentPackageRepository = new Mock<IRecentPackageRepository>();
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new MockFileSystem(), sharedRepository.Object,
                recentPackageRepository.Object, new VsPackageInstallerEvents(), new MockPackageRepository());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null, new Mock<IHttpClientEvents>().Object, null);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();


            // Assert
            sharedRepository.Verify();
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
                : base(new Mock<ISolutionManager>().Object, sourceRepository, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IRecentPackageRepository>().Object, new Mock<VsPackageInstallerEvents>().Object, new MockPackageRepository()) {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public SemanticVersion Version { get; set; }

            public bool IgnoreDependencies { get; set; }

            public override void InstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPreReleaseVersions, ILogger logger) {
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
