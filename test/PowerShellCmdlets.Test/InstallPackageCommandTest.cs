using System;
using System.Collections.Generic;
using System.IO;
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
    
    public class InstallPackageCommandTest
    {
        [Fact]
        public void InstallPackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, null, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void InstallPackageCmdletUsesPackageManangerWithSourceIfSpecified()
        {
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
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Source = "somesource";
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Same(sourceVsPackageManager, cmdlet.PackageManager);
        }

        [Fact]
        public void InstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);

            var cmdlet = new InstallPackageCommand(
                TestUtils.GetSolutionManager(), 
                packageManagerFactory.Object, 
                null, 
                new Mock<IVsPackageSourceProvider>().Object, 
                null, 
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object,
                true);
            cmdlet.Id = "my-id";
            cmdlet.Version = new SemanticVersion("2.8");

            // Act
            cmdlet.Execute();

            // Assert
            Assert.Equal("my-id", vsPackageManager.PackageId);
            Assert.Equal(new SemanticVersion("2.8"), vsPackageManager.Version);
        }

        [Fact]
        public void InstallPackageCmdletPassesIgnoreDependencySwitchCorrectly()
        {
            // Arrange
            var vsPackageManager = new MockVsPackageManager();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);

            var cmdlet = new InstallPackageCommand(
                TestUtils.GetSolutionManager(), 
                packageManagerFactory.Object, 
                null, 
                new Mock<IVsPackageSourceProvider>().Object, 
                null,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object, true);
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
        public void InstallPackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddress()
        {
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
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
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
        public void InstallPackageCmdletInvokeProductUpdateCheckWhenSourceIsHttpAddressAndSourceNameIsSpecified()
        {
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
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
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
        public void InstallPackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddress()
        {
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
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
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
        public void InstallPackageCmdletDoNotInvokeProductUpdateCheckWhenSourceIsNotHttpAddressAndSourceNameIsSpecified()
        {
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
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, packageRepositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
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
        public void InstallPackageCmdletCreatesFallbackRepository()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            IPackageRepository repoA = new MockPackageRepository(), repoB = new MockPackageRepository();
            var package = NuGet.Test.PackageUtility.CreatePackage("P1", dependencies: new[] { new PackageDependency("P2") });
            repoA.AddPackage(package);
            repoB.AddPackage(NuGet.Test.PackageUtility.CreatePackage("P2"));
            var sharedRepo = new Mock<ISharedPackageRepository>();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository("A")).Returns(repoA);
            repositoryFactory.Setup(c => c.CreateRepository("B")).Returns(repoB);
            var sourceProvider = GetPackageSourceProvider(new PackageSource("A"), new PackageSource("B"));
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(c => c.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(c => c.RepositoryPath).Returns(String.Empty);

            var solutionManager = new Mock<ISolutionManager>();
            var packageManagerFactory = new Mock<VsPackageManagerFactory>(
                solutionManager.Object, 
                repositoryFactory.Object, 
                sourceProvider, 
                fileSystemProvider.Object, 
                repositorySettings.Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                new Mock<IPackageRepository>().Object,
                /* multiFrameworkTargeting */ null,
                /* machineWideSettings */ null);
            packageManagerFactory.Setup(f => f.GetConfigSettingsFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, sourceProvider, null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "P1";
            cmdlet.Source = "A";

            // Act
            cmdlet.Execute();

            // Assert
            // If we've come this far, P1 is successfully installed.
            Assert.True(true);
        }

        [Fact]
        public void InstallPackageCmdletCreatesPackageManagerWithFallbackFlagSet()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var fallbackRepo = new Mock<IVsPackageManager>();
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(c => c.CreatePackageManager()).Returns(fallbackRepo.Object).Verifiable();
            packageManagerFactory.Setup(c => c.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Throws(new Exception());
            var repoA = new MockPackageRepository();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository("A")).Returns(repoA);

            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManagerWithProjects("foo"), packageManagerFactory.Object, repositoryFactory.Object, GetPackageSourceProvider(new PackageSource("A")), null, productUpdateService.Object, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "P1";
            cmdlet.Source = "A";

            // Act
            cmdlet.Execute();

            // Assert
            // If we've come this far, P1 is successfully installed.
            Assert.True(true);
        }

        [Fact]
        public void InstallPackageCmdletFallsbackToCacheWhenNetworkIsUnavailable()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var vsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var sourceVsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Returns(sourceVsPackageManager);

            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true)).Returns(new[]
            {
                new SettingValue("one", @"\\LetsHopeThisDirectory\IsNotAvaialble", false),
            });

            userSettings.Setup(s => s.GetValues("activePackageSource"))
                        .Returns(new[] { new KeyValuePair<string, string>("one", @"\\LetsHopeThisDirectory\IsNotAvaialble") });

            var provider = new VsPackageSourceProvider(userSettings.Object, CreateDefaultSourceProvider(userSettings.Object), new

Mock<IVsShellInfo>().Object);
            var activeSource = provider.ActivePackageSource;


            //Act           
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(),
                packageManagerFactory.Object, repositoryFactory.Object,
                provider, null, null,
                new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, false);
            cmdlet.Id = "my-id";
            cmdlet.Execute();

            // Assert
            Assert.Equal(cmdlet.Source, NuGet.MachineCache.Default.Source);
        }

        [Fact]
        public void FallbackToCacheDoesntHappenWhenAggregateIsUsedAndLocalSourceIsAvailable()
        {
            // Arrange
            string localdrive = System.Environment.GetEnvironmentVariable("TEMP");
            var userSettings = new Mock<ISettings>();
            userSettings.Setup(s => s.GetSettingValues("packageSources", true)).Returns(new[]
            {
                new SettingValue("one", @"\\LetsHopeThisDirectory\IsNotAvaialble", false),
                new SettingValue("two", localdrive, false),
                new SettingValue("three", @"http://SomeHttpSource/NotAvailable", false),
            });

            userSettings.Setup(s => s.GetValues("activePackageSource"))
                        .Returns(new[] { new KeyValuePair<string, string>("All", @"(All)"),
                                         });

            var provider = new VsPackageSourceProvider(userSettings.Object, CreateDefaultSourceProvider(userSettings.Object), new 
                Mock<IVsShellInfo>().Object);
            var activeSource = provider.ActivePackageSource;

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var vsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(vsPackageManager);
            var sourceVsPackageManager = new MockVsPackageManager();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Returns(sourceVsPackageManager);

            //Act           
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(),
                packageManagerFactory.Object, repositoryFactory.Object,
                provider, null, null,
                new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, false);
            cmdlet.Id = "my-id";
            cmdlet.Execute();

            // Assert
            Assert.NotEqual(cmdlet.Source, NuGet.MachineCache.Default.Source);
        }


        [Fact]
        public void InstallPackageCmdletDoesNotInstallPrereleasePackageIfFlagIsNotPresent()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0-a");
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);

            var packageRepository = new MockPackageRepository { packageA1 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, null);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.Execute(), "Unable to find package 'A'."); 
        }

        [Fact]
        public void InstallPackageCmdletInstallPrereleasePackageIfFlagIsPresent()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0-a");
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0.0-a"))).Returns(true);
            var packageRepository = new MockPackageRepository { packageA };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(
                TestUtils.GetSolutionManager(), 
                packageManagerFactory.Object, 
                null, 
                new Mock<IVsPackageSourceProvider>().Object, 
                new Mock<IHttpClientEvents>().Object, 
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object,
                true);

            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
        }

        [Fact]
        public void InstallPackageWithoutSettingVersionDoNotInstallUnlistedPackage()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0.0", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA1));
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA1), Times.Once());
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }

        [Fact]
        public void InstallPackageWithoutSettingVersionDoNotInstallUnlistedPackageWithPrerelease()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "1.0.1-alpha", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA1));
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();
            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA1), Times.Once());
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Never());
        }


        [Fact]
        public void InstallPackageInstallUnlistedPackageIfVersionIsSet()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0.0", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA2));
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("2.0.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("2.0.0");
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA1), Times.Never());
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Once());
        }

        [Fact]
        public void InstallPackageInstallUnlistedPrereleasePackageIfVersionIsSet()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "1.0.0-ReleaseCandidate", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA2));
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0.0-ReleaseCandidate"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("1.0.0-ReleaseCandidate");
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify(s => s.AddPackage(packageA1), Times.Never());
            sharedRepository.Verify(s => s.AddPackage(packageA2), Times.Once());
        }

        [Fact]
        public void InstallPackageInstallUnlistedPackageAsADependency()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0", dependencies: new [] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0.0", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB)).Verifiable();
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0.0"))).Returns(true);
            sharedRepository.Setup(s => s.IsReferenced("B", new SemanticVersion("1.0.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
        }

        [Theory]
        [InlineData("1.0.0", "1.0.0-gamma")]
        [InlineData("1.0.0-beta", "2.0.0")]
        public void InstallPackageInstallUnlistedPrereleasePackageAsADependency(string versionA, string versionB)
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", versionA, dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", versionB, listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB)).Verifiable();
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion(versionA))).Returns(true);
            sharedRepository.Setup(s => s.IsReferenced("B", new SemanticVersion(versionB))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
        }

        //Unit test for https://nuget.codeplex.com/workitem/3844
        [Fact]
        public void InstallPackageIgnoresFailingRepositoriesWhenInstallingPackageWithOrWithoutDependencies()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageC = PackageUtility.CreatePackage("C", "2.0.0");
            
            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageC)).Verifiable();

            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable()).Verifiable();
            var packageRepository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    packageA
                }, 
                mockRepository.Object,
                new MockPackageRepository { 
                   packageB 
                },
                new MockPackageRepository { 
                   packageC 
                },
            });
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();
            cmdlet.Id = "C";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
            mockRepository.Verify();
        }
            
        [Fact]
        public void InstallPackageShouldPickListedPackagesOverUnlistedOnesAsDependency()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("0.5")})});
            var packageB1 = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.2", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB1)).Verifiable();

            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
            sharedRepository.Verify(s => s.AddPackage(packageB2), Times.Never());
        }

        [Fact]
        public void InstallPackageShouldPickListedPackagesOverUnlistedOnesAsDependency2()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("0.5") }) });
            var packageB1 = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.2-alpha", listed: true);
            var packageB3 = PackageUtility.CreatePackage("B", "1.0.2", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB2)).Verifiable();

            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2, packageB3 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.DependencyVersion = DependencyVersion.HighestPatch;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
            sharedRepository.Verify(s => s.AddPackage(packageB1), Times.Never());
            sharedRepository.Verify(s => s.AddPackage(packageB3), Times.Never());
        }

        [Fact]
        public void InstallPackageShouldPickUnListedPackagesIfItSatisfiesContrainsAndOthersAreNot()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("1.0"), IsMinInclusive = true }) });
            var packageB1 = PackageUtility.CreatePackage("B", "0.0.9", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.0", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB2)).Verifiable();

            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
            sharedRepository.Verify(s => s.AddPackage(packageB1), Times.Never());
        }

        [Fact]
        public void InstallPackageShouldPickUnListedPrereleasePackagesIfItSatisfiesContrainsAndOthersAreNot()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("1.0"), IsMinInclusive = true }) });
            var packageB1 = PackageUtility.CreatePackage("B", "0.0.9", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.1-a", listed: false);
            var sharedRepository = new Mock<ISharedPackageRepository>();
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA)).Verifiable();
            sharedRepository.Setup(s => s.AddPackage(packageB2)).Verifiable();

            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object, new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(TestUtils.GetSolutionManager(), packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            sharedRepository.Verify();
            sharedRepository.Verify(s => s.AddPackage(packageB1), Times.Never());
        }

        [Fact]
        public void InstallPackageCmdletOpenReadmeFileFromPackageIfItIsPresent()
        {
            // Arrange
            var packageA = new Mock<IPackage>();
            packageA.Setup(p => p.Id).Returns("A");
            packageA.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            packageA.Setup(p => p.Listed).Returns(true);
            var readme = new Mock<IPackageFile>();
            readme.Setup(f => f.Path).Returns("readMe.txt");
            readme.Setup(f => f.GetStream()).Returns(new MemoryStream());
            packageA.Setup(p => p.GetFiles()).Returns(new IPackageFile[] { readme.Object });

            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA.Object)).Verifiable();
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA.Object };
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManagerWithProjects("foo"), packageRepository, new Mock<IFileSystemProvider>().Object, new MockFileSystem(), sharedRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object, new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(
                TestUtils.GetSolutionManager(), 
                packageManagerFactory.Object,
                null, 
                new Mock<IVsPackageSourceProvider>().Object,
                new Mock<IHttpClientEvents>().Object, 
                null, 
                fileOperations.Object,
                new Mock<IDeleteOnRestartManager>().Object,
                true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            fileOperations.Verify(io => io.OpenFile(It.Is<string>(s => s.EndsWith("A.1.0\\readme.txt", StringComparison.OrdinalIgnoreCase))), Times.Once());
        }

        [Fact]
        public void InstallPackageCmdletOnlyOpenReadmeFileFromTheRootPackage()
        {
            // Arrange
            // A --> B
            var packageA = new Mock<IPackage>();
            packageA.Setup(p => p.Id).Returns("A");
            packageA.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            var depSet = new PackageDependencySet(null, new[] { new PackageDependency("B") });
            packageA.Setup(p => p.DependencySets).Returns(new[] { depSet });
            packageA.Setup(p => p.Listed).Returns(true);
            var readme = new Mock<IPackageFile>();
            readme.Setup(f => f.Path).Returns("readMe.txt");
            readme.Setup(f => f.GetStream()).Returns(new MemoryStream());
            packageA.Setup(p => p.GetFiles()).Returns(new IPackageFile[] { readme.Object });

            var packageB = new Mock<IPackage>();
            packageB.Setup(p => p.Id).Returns("B");
            packageB.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            var readmeB = new Mock<IPackageFile>();
            readmeB.Setup(f => f.Path).Returns("readMe.txt");
            readmeB.Setup(f => f.GetStream()).Returns(new MemoryStream());
            packageB.Setup(p => p.GetFiles()).Returns(new IPackageFile[] { readmeB.Object });

            var sharedRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            sharedRepository.SetupSet(s => s.PackageSaveMode = PackageSaveModes.Nupkg);
            sharedRepository.Setup(s => s.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            sharedRepository.Setup(s => s.AddPackage(packageA.Object));
            sharedRepository.Setup(s => s.AddPackage(packageB.Object));
            sharedRepository.Setup(s => s.IsReferenced("A", new SemanticVersion("1.0"))).Returns(true);
            sharedRepository.Setup(s => s.IsReferenced("B", new SemanticVersion("1.0"))).Returns(true);

            var packageRepository = new MockPackageRepository { packageA.Object, packageB.Object };
            var packageManager = new VsPackageManager(
                TestUtils.GetSolutionManagerWithProjects("foo"), 
                packageRepository,
                new Mock<IFileSystemProvider>().Object,
                new MockFileSystem(),
                sharedRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object,
                new VsPackageInstallerEvents());
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(
                TestUtils.GetSolutionManager(), 
                packageManagerFactory.Object, 
                null,
                new Mock<IVsPackageSourceProvider>().Object, 
                new Mock<IHttpClientEvents>().Object, 
                null, 
                fileOperations.Object,
                new Mock<IDeleteOnRestartManager>().Object,
                true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            fileOperations.Verify(io => io.OpenFile(It.Is<string>(s => s.EndsWith("A.1.0\\readme.txt", StringComparison.OrdinalIgnoreCase))), Times.Once());
            fileOperations.Verify(io => io.OpenFile(It.Is<string>(s => s.EndsWith("B.1.0\\readme.txt", StringComparison.OrdinalIgnoreCase))), Times.Never());
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
                : base(new Mock<ISolutionManager>().Object, sourceRepository, new Mock<IFileSystemProvider>().Object, new Mock<IFileSystem>().Object, new Mock<ISharedPackageRepository>().Object, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object)
            {
            }

            public IProjectManager ProjectManager { get; set; }

            public string PackageId { get; set; }

            public SemanticVersion Version { get; set; }

            public bool IgnoreDependencies { get; set; }

            public override void InstallPackage(IProjectManager projectManager, string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPreReleaseVersions, ILogger logger)
            {
                ProjectManager = projectManager;
                PackageId = packageId;
                Version = version;
                IgnoreDependencies = ignoreDependencies;
            }

            public override IProjectManager GetProjectManager(Project project)
            {
                return new Mock<IProjectManager>().Object;
            }
        }

        private static PackageSourceProvider CreateDefaultSourceProvider(ISettings settings)
        {
            return new PackageSourceProvider(settings, VsPackageSourceProvider.DefaultSources, VsPackageSourceProvider.FeedsToMigrate);
        }

        private static IEnumerable<IPackage> GetPackagesWithException()
        {
            yield return PackageUtility.CreatePackage("A");
            throw new InvalidOperationException();
        }
    }
}