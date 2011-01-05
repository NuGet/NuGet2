using System;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.Cmdlets;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.Cmdlets.Test {

    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class GetPackageCmdletTest {
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
            cmdlet.Remote = new SwitchParameter(isPresent: true);

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
        public void GetPackageReturnsAllPackagesFromActiveRepositoryWhenRemoteIsPresentAndTheRemoteRepositoryIsThrottled() {
            // Arrange 
            var cmdlet = BuildCmdlet(throttling: true);
            cmdlet.Remote = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(100, result.Count());

            for (int i = 0; i < 100; i++) {
                AssertPackageResultsEqual(result.ElementAt(i), new { Id = "P" + i.ToString("00"), Version = new Version("1." + i.ToString("00")) });
            }
        }
        
        [TestMethod]
        public void GetPackageReturnsCorrectPackagesFromActiveRepositoryWhenRemoteAndSkipAndFirstIsPresent() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Remote = new SwitchParameter(isPresent: true);
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
            cmdlet.Remote = new SwitchParameter(isPresent: true);
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
            cmdlet.Remote = new SwitchParameter(isPresent: true);
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
            cmdlet.Remote = new SwitchParameter(isPresent: true);
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
            cmdlet.Remote = new SwitchParameter(isPresent: true);

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
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(() => GetPackageManager(false));
            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(m => m.RepositoryPath).Returns("foo");
            var cmdlet = new Mock<GetPackageCmdlet>(GetRepositoryFactory(), new Mock<IPackageSourceProvider>().Object, TestUtils.GetSolutionManager(isSolutionOpen: false), packageManagerFactory.Object) { CallBase = true }.Object;
            cmdlet.Remote = new SwitchParameter(isPresent: true);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(), "Unable to retrieve package list because no source was specified.");
        }

        private static void AssertPackageResultsEqual(dynamic a, dynamic b) {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Version, b.Version);
        }

        private static GetPackageCmdlet BuildCmdlet(bool isSolutionOpen = true, bool throttling = false) {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(() => GetPackageManager(throttling));
            return new GetPackageCmdlet(GetRepositoryFactory(), GetSourceProvider(), TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen), packageManagerFactory.Object);
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

        private static IVsPackageManager GetPackageManager(bool throttling = false) {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), throttling ? GetThrottledActiveRepository() : GetActiveRepository(), fileSystem.Object, localRepo.Object);
        }

        private static IPackageRepository GetActiveRepository() {
            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), 
                                         PackageUtility.CreatePackage("P2", "1.2"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return remoteRepo.Object;
        }

        private static IPackageRepository GetThrottledActiveRepository() {
            var remotePackages = new IPackage[100];
            for (int i = 0; i < remotePackages.Length; i++) {
                remotePackages[i] = PackageUtility.CreatePackage("P" + i.ToString("00"), "1." + i.ToString("00"));
            }

            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(new ThrottledQueryable<IPackage>(remotePackages));
            return remoteRepo.Object;
        }

        private static IPackageSourceProvider GetSourceProvider() {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("ActiveRepo", "ActiveRepo"));
            return sourceProvider.Object;
        }
    }
}