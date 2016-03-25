using System;
using System.Linq;
using System.Management.Automation;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;

namespace NuGet.PowerShell.Commands.Test
{

    using System.Collections.Generic;
    using PackageUtility = NuGet.Test.PackageUtility;


    public class GetPackageCommandTest
    {
        [Fact]
        public void GetPackageReturnsAllInstalledPackagesWhenNoParametersAreSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();

            // Act 
            var result = cmdlet.GetResults().Cast<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("0.9") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P2", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsCorrectInstalledPackagesWhenNoParametersAreSpecifiedAndSkipAndTakeAreSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.First = 1;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults().Cast<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P2", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsFilteredPackagesFromInstalledRepo()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "P2";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P2", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsAllPackagesFromActiveRepositoryWhenRemoteIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(4, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P2", Version = new SemanticVersion("1.2") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "P3", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsCorrectPackagesFromActiveRepositoryWhenRemoteAndSkipAndFirstIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 2;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P2", Version = new SemanticVersion("1.2") });
        }

        [Fact]
        public void GetPackageReturnsFilteredPackagesFromActiveRepositoryWhenRemoteIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P1";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new SemanticVersion("1.1") });
        }

        [Fact]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoFilterIsSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoFilterIsSpecifiedAndSourceNameIsUsed()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foosource";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageDoesNotReturnPackageFromDisabledSources()
        {
            // Arrange
            var packageSourceProvider = new Mock<IVsPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(
                new[]
                    {
                        new PackageSource("bing"),
                        new PackageSource("awesomesource", "awesomesource", isEnabled: false)
                    }
                );

            var cmdlet = BuildCmdlet(
                repositoryFactory:
                    new CachedRepositoryFactory(GetDefaultRepositoryFactory("All"), packageSourceProvider.Object),
                activeSourceName: "All");
            cmdlet.ListAvailable = new SwitchParameter(true);
            cmdlet.Source = "All";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoFilterIsSpecifiedAndRemoteIsNotSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsFilteredPackagesFromSpecifiedSourceWhenNoFilterIsSpecified()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P6";
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageThrowsWhenSolutionIsClosedAndRemoteIsNotPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                                                              "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void GetPackageReturnsPackagesFromRemoteWhenSolutionIsClosedAndRemoteIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(4, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P2", Version = new SemanticVersion("1.2") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "P3", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageReturnsPackagesFromRemoteWhenSolutionIsClosedAndSourceIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Source = "bing";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageThrowsWhenSolutionIsClosedAndUpdatesIsPresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                                                              "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void GetPackageThrowsWhenSolutionIsClosedUpdatesAndSourceArePresent()
        {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                                                              "The current environment doesn't have a solution open.");
        }

        [Fact]
        public void GetPackageReturnsListOfPackagesWithUpdates()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 2);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.Last(), new { Id = "P2", Version = new SemanticVersion("1.2") });
        }

        [Fact]
        public void GetPackageReturnsFilteredListOfPackagesWithUpdates()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P1";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new SemanticVersion("1.1") });
        }

        [Fact]
        public void GetPackageReturnsAllPackagesFromSourceWithUpdates()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
        }

        [Fact]
        public void GetPackageReturnsFilteredPackagesFromSourceWithUpdates()
        {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";
            cmdlet.Filter = "P1";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new SemanticVersion("1.4") });
        }

        [Fact]
        public void GetPackagesThrowsWhenNoSourceIsProvidedAndRemoteIsPresent()
        {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(() => GetPackageManager());
            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(m => m.RepositoryPath).Returns("foo");
            var cmdlet =
                new Mock<GetPackageCommand>(
                    GetDefaultRepositoryFactory(),
                    new Mock<IVsPackageSourceProvider>().Object,
                    TestUtils.GetSolutionManager(isSolutionOpen: false),
                    packageManagerFactory.Object,
                    null,
                    null) { CallBase = true }.Object;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                                                              "Unable to retrieve package list because no source was specified.");
        }

        [Fact]
        public void GetPackagesReturnsLatestPackageVersionByDefault()
        {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source),
                                     activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 3);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new SemanticVersion("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1),
                                      new { Id = "NHibernate", Version = new SemanticVersion("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "TestPack", Version = new SemanticVersion("0.7") });
        }

        [Fact]
        public void GetPackagesDoesNotCollapseVersionIfAllVersionsIsPresent()
        {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source),
                                     packageManagerFactory: GetPackageManagerForMultipleVersions(),
                                     activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.AllVersions = true;

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 6);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "JQuery", Version = new SemanticVersion("1.44") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "jQuery", Version = new SemanticVersion("1.51") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "jQuery", Version = new SemanticVersion("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(3),
                                      new { Id = "NHibernate", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(4),
                                      new { Id = "NHibernate", Version = new SemanticVersion("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(5), new { Id = "TestPack", Version = new SemanticVersion("0.7") });
        }

        [Fact]
        public void GetPackagesReturnsLatestUpdateVersions()
        {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source),
                                     packageManagerFactory: GetPackageManagerForMultipleVersions(),
                                     activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(packages.Count(), 2);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new SemanticVersion("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "TestPack", Version = new SemanticVersion("0.7") });
        }

        [Fact]
        public void GetPackageInvokeProductUpdateCheckWhenSourceIsHttp()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);
            cmdlet.Source = "http://bing.com";
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void GetPackageInvokeProductUpdateCheckWhenActiveSourceIsHttp()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(isSolutionOpen: false, productUpdateService: productUpdateService.Object,
                                     activeSourceName: "http://msn.com");
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [Fact]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenActiveSourceIsNotHttp()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(isSolutionOpen: false, productUpdateService: productUpdateService.Object);
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void GetPackageInvokeProductUpdateCheckWhenSourceIsNotHttp()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);
            cmdlet.Source = "foo";
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenGetLocalPackages()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenGetUpdatesPackages()
        {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);
            cmdlet.Updates = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [Fact]
        public void GetPackageReturnsPrereleasePackageForInstalledWhenFlagIsNotSpecified()
        {
            // Arrange
            var installedPackages = new[]
                                        {
                                            PackageUtility.CreatePackage("A"), PackageUtility.CreatePackage("B", "1.1.0-a")
                                            , PackageUtility.CreatePackage("C", "1.3.7.5")
                                        };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageManager = GetPackageManager(installedPackages);
            packageManagerFactory.Setup(f => f.CreatePackageManager()).Returns(packageManager);
            var cmdlet = BuildCmdlet(repositoryFactory: GetDefaultRepositoryFactory(),
                                     packageManagerFactory: packageManagerFactory.Object);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.0") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "B", Version = new SemanticVersion("1.1.0-a") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "C", Version = new SemanticVersion("1.3.7.5") });
        }

        [Fact]
        public void GetPackageDoesNotReturnsUpdatesForPrereleasePackagesWhenFlagIsNotSpecified()
        {
            // Arrange
            var installedPackages = new[]
                                        {
                                            PackageUtility.CreatePackage("A"), PackageUtility.CreatePackage("B", "1.1.0-a")
                                            , PackageUtility.CreatePackage("C", "1.3.7.5-a")
                                        };
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0"),
                                           PackageUtility.CreatePackage("C", "1.3.7.5-b"),
                                           PackageUtility.CreatePackage("D")
                                       };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            var packageManager = GetPackageManager(installedPackages);
            packageManagerFactory.Setup(f => f.CreatePackageManager()).Returns(packageManager);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);
            var cmdlet = BuildCmdlet(repositoryFactory: repositoryFactory.Object,
                                     packageManagerFactory: packageManagerFactory.Object);
            cmdlet.Updates = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "B", Version = new SemanticVersion("1.1.0") });
        }

        [Fact]
        public void GetPackageReturnsUpdatesForPrereleasePackagesWhenFlagIsSpecified()
        {
            // Arrange
            var installedPackages = new[]
                                        {
                                            PackageUtility.CreatePackage("A"), PackageUtility.CreatePackage("B", "1.1.0-a")
                                            , PackageUtility.CreatePackage("C", "1.3.7.5-a")
                                        };
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0"),
                                           PackageUtility.CreatePackage("C", "1.3.7.5-b"),
                                           PackageUtility.CreatePackage("D")
                                       };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            var packageManager = GetPackageManager(installedPackages);
            packageManagerFactory.Setup(f => f.CreatePackageManager()).Returns(packageManager);
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);
            var cmdlet = BuildCmdlet(repositoryFactory: repositoryFactory.Object,
                                     packageManagerFactory: packageManagerFactory.Object);
            cmdlet.Updates = true;
            cmdlet.IncludePrerelease = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "B", Version = new SemanticVersion("1.1.0") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "C", Version = new SemanticVersion("1.3.7.5-b") });
        }

        [Fact]
        public void GetPackageDoesNotListPrereleasePackagesWhenFlagIsNotSpecified()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0-b"),
                                           PackageUtility.CreatePackage("D")
                                       };
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);
            var cmdlet = BuildCmdlet(repositoryFactory: repositoryFactory.Object);
            cmdlet.ListAvailable = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "D", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageListPrereleasePackagesWhenFlagIsSpecified()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0-b"),
                                           PackageUtility.CreatePackage("D")
                                       };
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);
            var cmdlet = BuildCmdlet(repositoryFactory: repositoryFactory.Object);
            cmdlet.ListAvailable = true;
            cmdlet.IncludePrerelease = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(3, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "B", Version = new SemanticVersion("1.1.0-b") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "D", Version = new SemanticVersion("1.0") });
        }

        [Fact]
        public void GetPackageDoNotShowUnlistedPackages()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0-b"),
                                           PackageUtility.CreatePackage("C", listed: false),
                                           PackageUtility.CreatePackage("D", "2.0.3.4-alpha", listed: false)
                                       };
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);
            var cmdlet = BuildCmdlet(repositoryFactory: repositoryFactory.Object);
            cmdlet.ListAvailable = true;
            cmdlet.IncludePrerelease = true;
            cmdlet.AllVersions = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.Equal(2, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "A", Version = new SemanticVersion("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "B", Version = new SemanticVersion("1.1.0-b") });
        }

        [Fact]
        public void GetPackageDoNotShowUnlistedPackagesForUpdates()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("A", "1.1"),
                                           PackageUtility.CreatePackage("B", "1.1.0-b"),
                                           PackageUtility.CreatePackage("C", "2.0.0", listed: false),
                                           PackageUtility.CreatePackage("C", "2.0.3.4-alpha", listed: false)
                                       };

            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);            
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);

            var solutionManager = TestUtils.GetSolutionManager();

            var localPackages = new IPackage[]
                                    {
                                        PackageUtility.CreatePackage("C", "1.0.0")
                                    };
            var localRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            localRepository.SetupSet(p => p.PackageSaveMode = PackageSaveModes.Nupkg);
            localRepository.Setup(p => p.GetPackages()).Returns(localPackages.AsQueryable()).Verifiable();

            var packageManager = new VsPackageManager(
                solutionManager,
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                new Mock<IFileSystem>().Object,
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object,
                new VsPackageInstallerEvents());

            var mockPackageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            mockPackageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager).Verifiable();

            var cmdlet = new GetPackageCommand(
                repositoryFactory.Object,
                GetSourceProvider("NuGet Official Source"),
                solutionManager,
                mockPackageManagerFactory.Object,
                new Mock<IHttpClient>().Object,
                new Mock<IProductUpdateService>().Object);

            cmdlet.Updates = true;
            cmdlet.IncludePrerelease = true;
            cmdlet.AllVersions = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            localRepository.Verify();
            Assert.False(packages.Any());
        }

        [Fact]
        public void GetPackageDoNotShowUnlistedPackagesForUpdates2()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository()
                                       {
                                           PackageUtility.CreatePackage("C", "1.0.0", listed: true),
                                           PackageUtility.CreatePackage("C", "2.0.0", listed: false),
                                           PackageUtility.CreatePackage("C", "2.0.1", listed: true),
                                           PackageUtility.CreatePackage("C", "2.0.3.4-alpha", listed: false),
                                           PackageUtility.CreatePackage("C", "2.0.3.5-alpha", listed: true),
                                           PackageUtility.CreatePackage("C", "2.5.0", listed: false),
                                       };
            var repositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            repositoryFactory.Setup(r => r.CreateRepository("NuGet Official Source")).Returns(sourceRepository);

            var solutionManager = TestUtils.GetSolutionManager();

            var localPackages = new IPackage[]
                                    {
                                        PackageUtility.CreatePackage("C", "1.0.0")
                                    };
            var localRepository = new Mock<ISharedPackageRepository>(MockBehavior.Strict);
            localRepository.SetupSet(p => p.PackageSaveMode = PackageSaveModes.Nupkg);
            localRepository.Setup(p => p.GetPackages()).Returns(localPackages.AsQueryable()).Verifiable();

            var packageManager = new VsPackageManager(
                solutionManager,
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                new Mock<IFileSystem>().Object,
                localRepository.Object,
                new Mock<IDeleteOnRestartManager>().Object,
                new VsPackageInstallerEvents());

            var mockPackageManagerFactory = new Mock<IVsPackageManagerFactory>(MockBehavior.Strict);
            mockPackageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(packageManager).Verifiable();

            var cmdlet = new GetPackageCommand(
                repositoryFactory.Object,
                GetSourceProvider("NuGet Official Source"),
                solutionManager,
                mockPackageManagerFactory.Object,
                new Mock<IHttpClient>().Object,
                new Mock<IProductUpdateService>().Object);

            cmdlet.Updates = true;
            cmdlet.IncludePrerelease = true;
            cmdlet.AllVersions = true;
            cmdlet.Source = "NuGet Official Source";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            localRepository.Verify();
            Assert.Equal(2, packages.Count());
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "C", Version = new SemanticVersion("2.0.1") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "C", Version = new SemanticVersion("2.0.3.5-alpha") });
        }

        private static void AssertPackageResultsEqual(dynamic a, dynamic b)
        {
            if (a is PSObject)
            {
                a = (a as PSObject).BaseObject;
            }

            if (b is PSObject)
            {
                b = (b as PSObject).BaseObject;
            }

            Assert.Equal(a.Id, b.Id);
            Assert.Equal(a.Version, b.Version);
        }

        private static GetPackageCommand BuildCmdlet(
            bool isSolutionOpen = true,
            IProductUpdateService productUpdateService = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IVsPackageManagerFactory packageManagerFactory = null,
            string activeSourceName = "ActiveRepo")
        {
            if (packageManagerFactory == null)
            {
                var mockFactory = new Mock<IVsPackageManagerFactory>();
                mockFactory.Setup(m => m.CreatePackageManager()).Returns(() => GetPackageManager());

                packageManagerFactory = mockFactory.Object;
            }

            return new GetPackageCommand(
                repositoryFactory ?? GetDefaultRepositoryFactory(activeSourceName),
                GetSourceProvider(activeSourceName),
                TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen),
                packageManagerFactory,
                null,
                productUpdateService);
        }

        private static IPackageRepositoryFactory GetDefaultRepositoryFactory(string activeSourceName = "ActiveRepo")
        {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository(activeSourceName)).Returns(GetActiveRepository());
            repositoryFactory.Setup(c => c.CreateRepository("http://bing.com")).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository("foo")).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository("bing")).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository("foosource")).Returns(repository.Object);

            var extraRepository = new MockPackageRepository();
            extraRepository.AddPackage(PackageUtility.CreatePackage("awesome", "1.0"));
            repositoryFactory.Setup(c => c.CreateRepository("awesomesource")).Returns(extraRepository);

            return repositoryFactory.Object;
        }

        private static IPackageRepositoryFactory GetRepositoryFactoryWithMultiplePackageVersions(
            string sourceName = "MultiSource")
        {
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(sourceName)).Returns(GetRepositoryWithMultiplePackageVersions());
            return factory.Object;
        }

        private static IPackageRepository GetRepositoryWithMultiplePackageVersions(string sourceName = "MultiSource")
        {
            var repositoryWithMultiplePackageVersions = new Mock<IPackageRepository>();
            var packages = new[]
                               {
                                   // different case is intended to test that id is case-insensitive
                                   PackageUtility.CreatePackage("JQuery", "1.44"),
                                   PackageUtility.CreatePackage("jQuery", "1.51"),
                                   PackageUtility.CreatePackage("jQuery", "1.52"),
                                   PackageUtility.CreatePackage("NHibernate", "1.1"),
                                   PackageUtility.CreatePackage("NHibernate", "1.13"),
                                   PackageUtility.CreatePackage("TestPack", "0.7")
                               };
            repositoryWithMultiplePackageVersions.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            return repositoryWithMultiplePackageVersions.Object;
        }

        private static IVsPackageManager GetPackageManager(IEnumerable<IPackage> localPackages = null)
        {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            localPackages = localPackages ??
                            new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), GetActiveRepository(), new Mock<IFileSystemProvider>().Object, fileSystem.Object,
                                        localRepo.Object, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);
        }

        private static IVsPackageManagerFactory GetPackageManagerForMultipleVersions()
        {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[]
                                    {
                                        PackageUtility.CreatePackage("jQuery", "1.2"),
                                        PackageUtility.CreatePackage("TestPack", "0.1")
                                    };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), GetActiveRepository(),
                                                      new Mock<IFileSystemProvider>().Object,
                                                      fileSystem.Object, localRepo.Object,
                                                      new Mock<IDeleteOnRestartManager>().Object,
                                                      new Mock<VsPackageInstallerEvents>().Object);

            var factory = new Mock<IVsPackageManagerFactory>();
            factory.Setup(c => c.CreatePackageManager()).Returns(packageManager);

            return factory.Object;
        }

        private static IPackageRepository GetActiveRepository()
        {
            var remotePackages = new[]
                                     {
                                         PackageUtility.CreatePackage("P0", "1.1"),
                                         PackageUtility.CreatePackage("P1", "1.1"),
                                         PackageUtility.CreatePackage("P2", "1.2"), PackageUtility.CreatePackage("P3")
                                     };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return remoteRepo.Object;
        }

        private static IVsPackageSourceProvider GetSourceProvider(string activeSourceName)
        {
            Mock<IVsPackageSourceProvider> sourceProvider = new Mock<IVsPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource(activeSourceName,
                                                                                       activeSourceName));
            return sourceProvider.Object;
        }
    }
}