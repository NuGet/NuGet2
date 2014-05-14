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
    using NuGet.Resolver;
    using PackageUtility = NuGet.Test.PackageUtility;
    public class UpdatePackageCommandTest
    {
        private void InstallPackage(string id, SemanticVersion version, MockVsPackageManager2 packageManager)
        {   
            // Resolve the package to install
            IPackage package = PackageRepositoryHelper.ResolvePackage(
                packageManager.SourceRepository,
                packageManager.LocalRepository,
                id,
                version,
                allowPrereleaseVersions: false);

            // Resolve operations
            var resolver = new OperationResolver(packageManager);
            var projectManager = packageManager.GetProjectManager(packageManager.SolutionManager.GetProject("default"));
            var projectOperations = resolver.ResolveProjectOperations(
                UserOperation.Install,
                package, 
                new VirtualProjectManager(projectManager));
            var operations = resolver.ResolveFinalOperations(projectOperations);

            // Execute operations
            var operationExecutor = new OperationExecutor();
            operationExecutor.Execute(operations);
        }

        [Fact]
        public void UpdatePackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        /* !!!
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
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
            var cmdlet = new UpdatePackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.IgnoreDependencies = new SwitchParameter(isPresent: true);
            cmdlet.Source = source;
            cmdlet.ProjectName = "foo";

            // Act
            cmdlet.Execute();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        } */

        [Theory]
        [InlineData("1.0.0", "2.0.0-alpha")]
        [InlineData("1.0.0-beta", "2.0.0")]
        [InlineData("1.0.0-beta", "1.0.1-beta")]
        [InlineData("1.0.0", "1.0.1")]
        public void UpdatePackageDoNotUpdateToUnlistedPackageWithPrerelease(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1);
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage(packageA1.Id, packageA1.Version, packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.GetResults();

            // Assert: packageA1 is not updated to packageA2. 
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);
        }

        [Theory]
        [InlineData("1.0.0", "1.0.1-alpha")]
        [InlineData("1.0.0-beta", "1.0.9")]
        [InlineData("1.0.0-beta", "1.0.1-beta")]
        [InlineData("1.0.0", "1.0.1")]
        public void SafeUpdatePackageDoNotUpdateToUnlistedPackageWithPrerelease(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1);
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage(packageA1.Id, packageA1.Version, packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Safe = true;
            cmdlet.GetResults();

            // Assert: packageA1 is not updated to packageA2. 
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);
        }

        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("1.0.0", "1.0.1")]
        public void UpdatePackageDoNotUpdateToUnlistedPackage(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1);
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage(packageA1.Id, packageA1.Version, packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.GetResults();

            // Assert: packageA1 is not updated to packageA2. 
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);
        }

        [Theory]
        [InlineData("1.0.0", "1.0.0.2")]
        [InlineData("1.0.0", "1.0.1.3")]
        public void SafeUpdatePackageDoNotUpdateToUnlistedPackage(string versionA1, string versionA2)
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", versionA1);
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage(packageA1.Id, packageA1.Version, packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);
            
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.Safe = true;
            cmdlet.Execute();

            // Assert: Since packageA2 is unlisted, packageA1 is not updated to packageA2. 
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);
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
            var packageA1 = PackageUtility.CreatePackage("A", versionA1);
            var packageA2 = PackageUtility.CreatePackage("A", versionA2, listed: false);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            InstallPackage(packageA1.Id, packageA1.Version, packageManager);
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA1 }, installedPackages, PackageEqualityComparer.IdAndVersion);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new UpdatePackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion(versionA2);
            cmdlet.GetResults();

            // Assert: packageA1 is updated to packageA2. 
            installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(new[] { packageA2 }, installedPackages, PackageEqualityComparer.IdAndVersion);
        }
    }
}