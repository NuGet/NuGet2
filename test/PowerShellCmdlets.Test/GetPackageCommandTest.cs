using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.PowerShell.Commands.Test {

    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class GetPackageCommandTest {
        [TestMethod]
        public void GetPackageReturnsAllInstalledPackagesWhenNoParametersAreSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();

            // Act 
            var result = cmdlet.GetResults().Cast<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new Version("0.9") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P2", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsCorrectInstalledPackagesWhenNoParametersAreSpecifiedAndSkipAndTakeAreSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.First = 1;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults().Cast<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P2", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromInstalledRepo() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "P2";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P2", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromActiveRepositoryWhenRemoteIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(4, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P2", Version = new Version("1.2") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "P3", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsCorrectPackagesFromActiveRepositoryWhenRemoteAndSkipAndFirstIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 2;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P2", Version = new Version("1.2") });
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromActiveRepositoryWhenRemoteIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P1";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new Version("1.1") });
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoFilterIsSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new Version("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromSpecifiedSourceWhenNoFilterIsSpecifiedAndRemoteIsNotSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new Version("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromSpecifiedSourceWhenNoFilterIsSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P6";
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P6", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndRemoteIsNotPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void GetPackageReturnsPackagesFromRemoteWhenSolutionIsClosedAndRemoteIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(4, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P2", Version = new Version("1.2") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "P3", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageReturnsPackagesFromRemoteWhenSolutionIsClosedAndSourceIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Source = "foo";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "P1", Version = new Version("1.4") });
            AssertPackageResultsEqual(result.Last(), new { Id = "P6", Version = new Version("1.0") });
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedAndUpdatesIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void GetPackageThrowsWhenSolutionIsClosedUpdatesAndSourceArePresent() {
            // Arrange 
            var cmdlet = BuildCmdlet(isSolutionOpen: false);
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void GetPackageReturnsListOfPackagesWithUpdates() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 2);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(packages.Last(), new { Id = "P2", Version = new Version("1.2") });
        }

        [TestMethod]
        public void GetPackageReturnsFilteredListOfPackagesWithUpdates() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "P1";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new Version("1.1") });
        }

        [TestMethod]
        public void GetPackageReturnsAllPackagesFromSourceWithUpdates() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new Version("1.4") });
        }

        [TestMethod]
        public void GetPackageReturnsFilteredPackagesFromSourceWithUpdates() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Source = "foo";
            cmdlet.Filter = "P1";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "P1", Version = new Version("1.4") });
        }

        [TestMethod]
        public void GetPackagesThrowsWhenNoSourceIsProvidedAndRemoteIsPresent() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);
            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(m => m.RepositoryPath).Returns("foo");
            var cmdlet = new Mock<GetPackageCommand>(GetDefaultRepositoryFactory(), new Mock<IPackageSourceProvider>().Object, TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, new Mock<IPackageRepository>().Object, null, null) { CallBase = true }.Object;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(), "Unable to retrieve package list because no source was specified.");
        }

        [TestMethod]
        public void GetPackagesReturnsLatestPackageVersionByDefault() {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source), activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 3);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "NHibernate", Version = new Version("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "TestPack", Version = new Version("0.7") });
        }

        [TestMethod]
        public void GetPackagesDoesNotCollapseVersionIfAllVersionsIsPresent() {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source), packageManagerFactory: GetPackageManagerForMultipleVersions(),
                activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.AllVersions = true;

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 6);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.44") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "jQuery", Version = new Version("1.51") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(3), new { Id = "NHibernate", Version = new Version("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(4), new { Id = "NHibernate", Version = new Version("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(5), new { Id = "TestPack", Version = new Version("0.7") });
        }

        [TestMethod]
        public void GetPackagesReturnsLatestUpdateVersions() {
            // Arrange
            var source = "http://multi-source";
            var cmdlet = BuildCmdlet(repositoryFactory: GetRepositoryFactoryWithMultiplePackageVersions(source), packageManagerFactory: GetPackageManagerForMultipleVersions(),
                activeSourceName: source);
            cmdlet.Source = source;
            cmdlet.Updates = new SwitchParameter(isPresent: true);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 2);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "TestPack", Version = new Version("0.7") });
        }

        [TestMethod]
        public void RecentPackagesCollapsesVersion() {
            // Arrange
            var cmdlet = BuildCmdlet(repositoryFactory: GetDefaultRepositoryFactory(), recentPackageRepository: GetRepositoryWithMultiplePackageVersions());
            cmdlet.Recent = new SwitchParameter(isPresent: true);

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 3);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "NHibernate", Version = new Version("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "TestPack", Version = new Version("0.7") });
        }

        [TestMethod]
        public void RecentPackagesFiltersAndCollapsesVersion() {
            // Arrange
            var cmdlet = BuildCmdlet(repositoryFactory: GetDefaultRepositoryFactory(), recentPackageRepository: GetRepositoryWithMultiplePackageVersions());
            cmdlet.Recent = true;
            cmdlet.Filter = "NHibernate jQuery";

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 2);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "NHibernate", Version = new Version("1.13") });
        }

        [TestMethod]
        public void RecentPackagesShowsAllPackageVersionsIfSwitchIsPresent() {
            // Arrange
            var cmdlet = BuildCmdlet(repositoryFactory: GetDefaultRepositoryFactory(), recentPackageRepository: GetRepositoryWithMultiplePackageVersions());
            cmdlet.Recent = true;
            cmdlet.AllVersions = true;

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 6);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.44") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "jQuery", Version = new Version("1.51") });
            AssertPackageResultsEqual(packages.ElementAt(2), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(3), new { Id = "NHibernate", Version = new Version("1.1") });
            AssertPackageResultsEqual(packages.ElementAt(4), new { Id = "NHibernate", Version = new Version("1.13") });
            AssertPackageResultsEqual(packages.ElementAt(5), new { Id = "TestPack", Version = new Version("0.7") });
        }

        [TestMethod]
        public void RecentPackagesWithShowAllVersionsAppliesFirstAndSkipFilters() {
            // Arrange
            var cmdlet = BuildCmdlet(repositoryFactory: GetDefaultRepositoryFactory(), recentPackageRepository: GetRepositoryWithMultiplePackageVersions());
            cmdlet.Recent = true;
            cmdlet.AllVersions = true;
            cmdlet.Skip = 2;
            cmdlet.First = 2;

            // Act 
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 2);
            AssertPackageResultsEqual(packages.ElementAt(0), new { Id = "jQuery", Version = new Version("1.52") });
            AssertPackageResultsEqual(packages.ElementAt(1), new { Id = "NHibernate", Version = new Version("1.1") });
        }

        [TestMethod]
        public void TestRecentSwitchWorkCorrectly() {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var repository = new MockPackageRepository();
            repository.Add(packageA);
            repository.Add(packageB);
            repository.Add(packageC);

            var cmdlet = BuildCmdlet(false, repository);
            cmdlet.Recent = true;

            // Act
            var packages = cmdlet.GetResults().OfType<IPackage>().ToList();

            // Assert
            Assert.AreEqual(3, packages.Count);
            Assert.AreSame(packageA, packages[0]);
            Assert.AreSame(packageB, packages[1]);
            Assert.AreSame(packageC, packages[2]);
        }

        [TestMethod]
        public void GetPackageInvokeProductUpdateCheckWhenSourceIsHttp() {
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

        [TestMethod]
        public void GetPackageInvokeProductUpdateCheckWhenActiveSourceIsHttp() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(isSolutionOpen: false, productUpdateService: productUpdateService.Object, activeSourceName: "http://msn.com");
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Once());
        }

        [TestMethod]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenActiveSourceIsNotHttp() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(isSolutionOpen: false, productUpdateService: productUpdateService.Object);
            cmdlet.ListAvailable = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [TestMethod]
        public void GetPackageInvokeProductUpdateCheckWhenSourceIsNotHttp() {
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

        [TestMethod]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenGetLocalPackages() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [TestMethod]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenGetUpdatesPackages() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);
            cmdlet.Updates = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        [TestMethod]
        public void GetPackageDoNotInvokeProductUpdateCheckWhenGetRecentPackages() {
            // Arrange
            var productUpdateService = new Mock<IProductUpdateService>();
            var cmdlet = BuildCmdlet(productUpdateService: productUpdateService.Object);
            cmdlet.Recent = true;

            // Act
            var packages = cmdlet.GetResults();

            // Assert
            productUpdateService.Verify(p => p.CheckForAvailableUpdateAsync(), Times.Never());
        }

        private static void AssertPackageResultsEqual(dynamic a, dynamic b) {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Version, b.Version);
        }

        private static GetPackageCommand BuildCmdlet(
            bool isSolutionOpen = true,
            IPackageRepository recentPackageRepository = null,
            IProductUpdateService productUpdateService = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IVsPackageManagerFactory packageManagerFactory = null,
            string activeSourceName = "ActiveRepo") {

            if (packageManagerFactory == null) {
                var mockFactory = new Mock<IVsPackageManagerFactory>();
                mockFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);

                packageManagerFactory = mockFactory.Object;
            }

            if (recentPackageRepository == null) {
                recentPackageRepository = new Mock<IPackageRepository>().Object;
            }

            return new GetPackageCommand(
                repositoryFactory ?? GetDefaultRepositoryFactory(activeSourceName),
                GetSourceProvider(activeSourceName),
                TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen),
                packageManagerFactory,
                recentPackageRepository,
                null,
                productUpdateService);
        }

        private static IPackageRepositoryFactory GetDefaultRepositoryFactory(string activeSourceName = "ActiveRepo") {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource("http://bing.com", "http://bing.com"))).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource("foo", "foo"))).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource(activeSourceName, activeSourceName))).Returns(GetActiveRepository());

            return repositoryFactory.Object;
        }

        private static IPackageRepositoryFactory GetRepositoryFactoryWithMultiplePackageVersions(string sourceName = "MultiSource") {
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(new PackageSource(sourceName, sourceName))).Returns(GetRepositoryWithMultiplePackageVersions());

            return factory.Object;
        }

        private static IPackageRepository GetRepositoryWithMultiplePackageVersions(string sourceName = "MultiSource") {
            var repositoryWithMultiplePackageVersions = new Mock<IPackageRepository>();
            var packages = new[] { 
                PackageUtility.CreatePackage("jQuery", "1.44"), 
                PackageUtility.CreatePackage("jQuery", "1.51"),
                PackageUtility.CreatePackage("jQuery", "1.52"),
                PackageUtility.CreatePackage("NHibernate", "1.1"),
                PackageUtility.CreatePackage("NHibernate", "1.13"),
                PackageUtility.CreatePackage("TestPack", "0.7")
            };
            repositoryWithMultiplePackageVersions.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            return repositoryWithMultiplePackageVersions.Object;
        }

        private static IPackageRepositoryFactory GetRepositoryFactory(IDictionary<string, IPackageRepository> repository) {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            repositoryFactory.Setup(c => c.CreateRepository(It.IsAny<PackageSource>())).Returns<PackageSource>(p => repository[p.Name]);

            return repositoryFactory.Object;
        }

        private static IVsPackageManager GetPackageManager() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), GetActiveRepository(), fileSystem.Object, localRepo.Object, new Mock<IRecentPackageRepository>().Object);
        }

        private static IVsPackageManagerFactory GetPackageManagerForMultipleVersions() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("jQuery", "1.2"), PackageUtility.CreatePackage("TestPack", "0.1") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), GetActiveRepository(), fileSystem.Object, localRepo.Object, new Mock<IRecentPackageRepository>().Object);

            var factory = new Mock<IVsPackageManagerFactory>();
            factory.Setup(c => c.CreatePackageManager()).Returns(packageManager);

            return factory.Object;
        }

        private static IPackageRepository GetActiveRepository() {
            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), 
                                         PackageUtility.CreatePackage("P2", "1.2"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return remoteRepo.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(string activeSourceName) {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource(activeSourceName, activeSourceName));
            return sourceProvider.Object;
        }
    }
}