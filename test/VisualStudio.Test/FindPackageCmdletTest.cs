using System;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio.Cmdlets;

namespace NuGet.VisualStudio.Test {
    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class FindPackageCmdletTest  {
        [TestMethod]
        public void FindPackageFiltersByIdWhenSwitchIsSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "pac";

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Pack2", Version = new Version("1.0") });
        }
        
        [TestMethod]
        public void FindPackageFiltersRemoteByIdWhenSwitchIsSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "pac";
            cmdlet.Remote = new SwitchParameter(isPresent: true);

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(1, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Pack2", Version = new Version("1.2") });
        }

        [TestMethod]
        public void FindPackageReturnsPackagesFilteredByIdWithUpdates() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Updates = new SwitchParameter(isPresent: true);
            cmdlet.Filter = "Pac";

            // Act
            var packages = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(packages.Count(), 1);
            AssertPackageResultsEqual(packages.First(), new { Id = "Pack2", Version = new Version("1.2") });
        }


        private static void AssertPackageResultsEqual(dynamic a, dynamic b) {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Version, b.Version);
        }

        private static FindPackage BuildCmdlet(bool isSolutionOpen = true) {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);
            return new FindPackage(GetRepositoryFactory(), GetSourceProvider(), TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen), packageManagerFactory.Object);
        }

        private static IPackageRepositoryFactory GetRepositoryFactory() {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            var packages = new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
            repository.Setup(c => c.GetPackages()).Returns(packages.AsQueryable());

            repositoryFactory.Setup(c => c.CreateRepository(new PackageSource("foo", "foo"))).Returns(repository.Object);

            return repositoryFactory.Object;
        }

        private static IVsPackageManager GetPackageManager() {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            var localPackages = new[] { PackageUtility.CreatePackage("P1", "0.9"), PackageUtility.CreatePackage("Pack2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            var remotePackages = new[] { PackageUtility.CreatePackage("P0", "1.1"), PackageUtility.CreatePackage("P1", "1.1"), 
                                         PackageUtility.CreatePackage("Pack2", "1.2"), PackageUtility.CreatePackage("P3") };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return new VsPackageManager(TestUtils.GetSolutionManager(), remoteRepo.Object, fileSystem.Object, localRepo.Object);
        }

        private static IPackageSourceProvider GetSourceProvider() {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("foo", "foo"));
            return sourceProvider.Object;
        }
    }
}
