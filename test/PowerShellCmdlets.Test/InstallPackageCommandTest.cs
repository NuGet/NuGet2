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
    using Microsoft.VisualStudio.Shell.Interop;
    using PackageUtility = NuGet.Test.PackageUtility;
    
    public class InstallPackageCommandTest
    {
        [Fact]
        public void InstallPackageCmdletThrowsWhenSolutionIsClosed()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0");
            var packageRepository = new MockPackageRepository { packageA };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);
            ((MockSolutionManager2)packageManager.SolutionManager).IsSolutionOpen = false;
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        /* !!!
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
        */

        [Fact]
        public void InstallPackageCmdletPassesParametersCorrectlyWhenIdAndVersionAreSpecified()
        {
            // Arrange
            var packageA28 = PackageUtility.CreatePackage("A", "2.8");
            var packageA31 = PackageUtility.CreatePackage("A", "3.1");
            var packageRepository = new MockPackageRepository { packageA28, packageA31 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(
                packageManager.SolutionManager,
                packageManagerFactory.Object,
                null,
                new Mock<IVsPackageSourceProvider>().Object,
                null,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("2.8");
            cmdlet.Execute();

            // Assert: version 2.8 is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA28, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageCmdletPassesIgnoreDependencySwitchCorrectly()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0.0");
            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(
                packageManager.SolutionManager, 
                packageManagerFactory.Object, 
                null, 
                new Mock<IVsPackageSourceProvider>().Object, 
                null,
                null,
                new Mock<IVsCommonOperations>().Object,
                new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IgnoreDependencies = new SwitchParameter(true);
            cmdlet.Execute();

            // Assert: only packageA is installed. packageB is not.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        /* !!!
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
        } */

        internal class TestVsPackageManagerFactory : VsPackageManagerFactory
        {
            public TestVsPackageManagerFactory(
                ISolutionManager solutionManager,
                IPackageRepositoryFactory repositoryFactory,
                IVsPackageSourceProvider packageSourceProvider,
                IFileSystemProvider fileSystemProvider,
                IRepositorySettings repositorySettings,
                VsPackageInstallerEvents packageEvents,
                IPackageRepository activePackageSourceRepository,
                IVsFrameworkMultiTargeting frameworkMultiTargeting,
                IMachineWideSettings machineWideSettings)
                : base(solutionManager, repositoryFactory, packageSourceProvider, fileSystemProvider, repositorySettings, packageEvents, activePackageSourceRepository, frameworkMultiTargeting, machineWideSettings)
            {
            }

            protected internal override IFileSystem GetConfigSettingsFileSystem(string configFolderPath)
            {
                return new MockFileSystem(configFolderPath);
            }
        }

        /* !!!
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
        } */

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
            var packageRepository = new MockPackageRepository { packageA };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution", 
                packageRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // packageA is not installed yet.
            Assert.False(packageManager.LocalRepository.IsSolutionReferenced(packageA.Id, packageA.Version));

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);            

            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert: packageA is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageWithoutSettingVersionDoNotInstallUnlistedPackage()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0.0", listed: false);            
            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert: packageA1 is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA1, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageWithoutSettingVersionDoNotInstallUnlistedPackageWithPrerelease()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "1.0.1-alpha", listed: false);            
            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();
            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert: packageA1 is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA1, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageInstallUnlistedPackageIfVersionIsSet()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0.0", listed: false);
            var packageRepository = new MockPackageRepository { packageA1, packageA2 };

            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("2.0.0");
            cmdlet.Execute();

            // Assert: the unlisted packageA2 is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA2, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageInstallUnlistedPrereleasePackageIfVersionIsSet()
        {
            // Arrange
            var packageA1 = PackageUtility.CreatePackage("A", "1.0.0");
            var packageA2 = PackageUtility.CreatePackage("A", "1.0.0-ReleaseCandidate", listed: false);
            
            var packageRepository = new MockPackageRepository { packageA1, packageA2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Version = new SemanticVersion("1.0.0-ReleaseCandidate");
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert: the unlisted prerelease packageA2 is installed.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(1, installedPackages.Count);
            Assert.Equal(packageA2, installedPackages[0], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageInstallUnlistedPackageAsADependency()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0.0", dependencies: new [] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0.0", listed: false);

            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert
            var installedPackages = packageManager.LocalRepository.GetPackages().OrderBy(p => p.Id).ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB, installedPackages[1], PackageEqualityComparer.IdAndVersion);
        }

        [Theory]
        [InlineData("1.0.0", "1.0.0-gamma")]
        [InlineData("1.0.0-beta", "2.0.0")]
        public void InstallPackageInstallUnlistedPrereleasePackageAsADependency(string versionA, string versionB)
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", versionA, dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", versionB, listed: false);
            
            var packageRepository = new MockPackageRepository { packageA, packageB };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB, installedPackages[1], PackageEqualityComparer.IdAndVersion);
        }

        //Unit test for https://nuget.codeplex.com/workitem/3844
        [Fact]
        public void InstallPackageIgnoresFailingRepositoriesWhenInstallingPackageWithOrWithoutDependencies()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageC = PackageUtility.CreatePackage("C", "2.0.0");
            
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

            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();
            cmdlet.Id = "C";
            cmdlet.Execute();

            // Assert            
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(3, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB, installedPackages[1], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageC, installedPackages[2], PackageEqualityComparer.IdAndVersion);

            mockRepository.Verify();
        }
            
        [Fact]
        public void InstallPackageShouldPickListedPackagesOverUnlistedOnesAsDependency()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("0.5")})});
            var packageB1 = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.2", listed: false);            
            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new MockVsPackageManager2(
               @"c:\solution",
               packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert: packageA and packageB1 are installed. packageB1 is picked because
            // packageB2 is unlisted.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB1, installedPackages[1], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageShouldPickListedPackagesOverUnlistedOnesAsDependency2()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("0.5") }) });
            var packageB1 = PackageUtility.CreatePackage("B", "1.0.0", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.2-alpha", listed: true);
            var packageB3 = PackageUtility.CreatePackage("B", "1.0.2", listed: false);
            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2, packageB3 };
            var packageManager = new MockVsPackageManager2(
               @"c:\solution",
               packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.DependencyVersion = DependencyVersion.HighestPatch;
            cmdlet.Execute();

            // Assert: packageA and packageB2 are installed.
            // packageB1 is not picked because packageB2's version is later.
            // packageB3 is not picked because it's unlisted.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB2, installedPackages[1], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageShouldPickUnListedPackagesIfItSatisfiesContrainsAndOthersAreNot()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("1.0"), IsMinInclusive = true }) });
            var packageB1 = PackageUtility.CreatePackage("B", "0.0.9", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.0", listed: false);
            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new MockVsPackageManager2(
               @"c:\solution",
               packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.Execute();

            // Assert: packageA and packageB2 are installed.
            // packageB1 is not picked because it cannot be used as a dependency of packageA.
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB2, installedPackages[1], PackageEqualityComparer.IdAndVersion);
        }

        [Fact]
        public void InstallPackageShouldPickUnListedPrereleasePackagesIfItSatisfiesContrainsAndOthersAreNot()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new[] { new PackageDependency("B", new VersionSpec { MinVersion = new SemanticVersion("1.0"), IsMinInclusive = true }) });
            var packageB1 = PackageUtility.CreatePackage("B", "0.0.9", listed: true);
            var packageB2 = PackageUtility.CreatePackage("B", "1.0.1-a", listed: false);
            var packageRepository = new MockPackageRepository { packageA, packageB1, packageB2 };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            // Act
            var cmdlet = new InstallPackageCommand(packageManager.SolutionManager, packageManagerFactory.Object, null, new Mock<IVsPackageSourceProvider>().Object, new Mock<IHttpClientEvents>().Object, null, new Mock<IVsCommonOperations>().Object, new Mock<IDeleteOnRestartManager>().Object, true);
            cmdlet.Id = "A";
            cmdlet.IncludePrerelease = true;
            cmdlet.Execute();

            // Assert: packageA and packageB2 are installed.
            // packageB1 is not picked because it cannot be used as a dependency of packageA
            var installedPackages = packageManager.LocalRepository.GetPackages().ToList();
            Assert.Equal(2, installedPackages.Count);
            Assert.Equal(packageA, installedPackages[0], PackageEqualityComparer.IdAndVersion);
            Assert.Equal(packageB2, installedPackages[1], PackageEqualityComparer.IdAndVersion);
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
            packageA.Setup(p => p.GetStream()).Returns(new MemoryStream());

            var packageRepository = new MockPackageRepository { packageA.Object };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(
                packageManager.SolutionManager, 
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
            packageA.Setup(p => p.GetStream()).Returns(new MemoryStream());

            var packageB = new Mock<IPackage>();
            packageB.Setup(p => p.Id).Returns("B");
            packageB.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            var readmeB = new Mock<IPackageFile>();
            readmeB.Setup(f => f.Path).Returns("readMe.txt");
            readmeB.Setup(f => f.GetStream()).Returns(new MemoryStream());
            packageB.Setup(p => p.GetFiles()).Returns(new IPackageFile[] { readmeB.Object });
            packageB.Setup(p => p.GetStream()).Returns(new MemoryStream());

            var packageRepository = new MockPackageRepository { packageA.Object, packageB.Object };
            var packageManager = new MockVsPackageManager2(
                @"c:\solution",
                packageRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager);

            var fileOperations = new Mock<IVsCommonOperations>();

            // Act
            var cmdlet = new InstallPackageCommand(
                packageManager.SolutionManager, 
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

        private static IEnumerable<IPackage> GetPackagesWithException()
        {
            yield return PackageUtility.CreatePackage("A");
            throw new InvalidOperationException();
        }
    }
}