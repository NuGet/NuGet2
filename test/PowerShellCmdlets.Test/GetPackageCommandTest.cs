using System;
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
            var cmdlet = new Mock<GetPackageCommand>(GetRepositoryFactory(), new Mock<IPackageSourceProvider>().Object, TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object, new Mock<IPackageRepository>().Object, null, null) { CallBase = true }.Object;
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(), "Unable to retrieve package list because no source was specified.");
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

        private static void AssertPackageResultsEqual(dynamic a, dynamic b) {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Version, b.Version);
        }

        private static GetPackageCommand BuildCmdlet(bool isSolutionOpen = true, IPackageRepository recentPackageRepository = null) {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);

            if (recentPackageRepository == null) {
                recentPackageRepository = new Mock<IPackageRepository>().Object;
            }

            return new GetPackageCommand(
                GetRepositoryFactory(), 
                GetSourceProvider(), 
                TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen), 
                packageManagerFactory.Object,
                recentPackageRepository,
                null,
                null);
        }

        private static IPackageRepositoryFactory GetRepositoryFactory() {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource("foo", "foo"))).Returns(repository.Object);
            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource("ActiveRepo", "ActiveRepo"))).Returns(GetActiveRepository());

            return repositoryFactory.Object;
        }

        private static IVsPackageManager GetPackageManager() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), GetActiveRepository(), fileSystem.Object, localRepo.Object, new Mock<IRecentPackageRepository>().Object);
        }

        private static IPackageRepository GetActiveRepository() {
            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), 
                                         PackageUtility.CreatePackage("P2", "1.2"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return remoteRepo.Object;
        }

        private static IPackageSourceProvider GetSourceProvider() {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("ActiveRepo", "ActiveRepo"));
            return sourceProvider.Object;
        }
    }
}