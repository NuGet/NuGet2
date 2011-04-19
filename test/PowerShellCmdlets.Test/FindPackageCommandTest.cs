using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.PowerShell.Commands.Test {
    
    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class FindPackageCommandTest {
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
        public void FindPackageReturnsMaximumResultsWithFirstAndSkipParametersSet() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 2;
            cmdlet.Skip = 1;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(3, result.Count());     // FindPackage always sets First = 30
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P3", Version = new Version("1.0") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "Pack2", Version = new Version("1.2") });
        }

        [TestMethod]
        public void FindPackageReturnsMaximumResultsWithFirstParameterSet() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);
            cmdlet.First = 20;

            // Act 
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(4, result.Count());     // FindPackage always sets First = 30
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "P0", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "P1", Version = new Version("1.1") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "P3", Version = new Version("1.0") });
            AssertPackageResultsEqual(result.ElementAt(3), new { Id = "Pack2", Version = new Version("1.2") });
        }

        [TestMethod]
        public void FindPackageFiltersRemoteByIdWhenSwitchIsSpecified() {
            // Arrange 
            var cmdlet = BuildCmdlet();
            cmdlet.Filter = "pac";
            cmdlet.ListAvailable = new SwitchParameter(isPresent: true);

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

        [TestMethod]
        public void FindPackageReturnsAllVersionsForSpecificPackage() {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };
                

            var cmdlet = BuildCmdlet(packages: packages);
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new Version("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new Version("0.4") });
        }

        [TestMethod]
        public void FindPackageReturnsPerformsPartialSearchesByDefault() {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };


            var cmdlet = BuildCmdlet(packages: packages);
            cmdlet.Filter = "Awe";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(3, result.Count());
            AssertPackageResultsEqual(result.ElementAt(0), new { Id = "Awesome", Version = new Version("0.1") });
            AssertPackageResultsEqual(result.ElementAt(1), new { Id = "Awesome", Version = new Version("0.4") });
            AssertPackageResultsEqual(result.ElementAt(2), new { Id = "AwesomeToo", Version = new Version("0.4") });
        }

        [TestMethod]
        public void FindPackagePerformsExactMatchesIfExactMatchIsSpecified() {
            // Arrange 
            var packages = new[] { 
                PackageUtility.CreatePackage("Awesome", "0.1", description: "some desc"),
                PackageUtility.CreatePackage("Awesome", "0.4", description: "some desc"),
                PackageUtility.CreatePackage("AwesomeToo", "0.4", description: "Awesome Too desc"),
                PackageUtility.CreatePackage("Foobar", "0.4", description: "Awesome"),
                PackageUtility.CreatePackage("Not-Awesome", "0.6", description: "Awesome"),
            };


            var cmdlet = BuildCmdlet(packages: packages);
            cmdlet.ExactMatch = true;
            cmdlet.Filter = "Awesome";
            cmdlet.Source = "foo";

            // Act
            var result = cmdlet.GetResults<dynamic>();

            // Assert
            Assert.AreEqual(2, result.Count());
            AssertPackageResultsEqual(result.First(), new { Id = "Awesome", Version = new Version("0.1") });
            AssertPackageResultsEqual(result.Last(), new { Id = "Awesome", Version = new Version("0.4") });
        }


        private static void AssertPackageResultsEqual(dynamic a, dynamic b) {
            Assert.AreEqual(a.Id, b.Id);
            Assert.AreEqual(a.Version, b.Version);
        }

        private static FindPackageCommand BuildCmdlet(bool isSolutionOpen = true, IEnumerable<IPackage> packages = null) {
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns(GetPackageManager);
            return new FindPackageCommand(
                GetRepositoryFactory(packages),
                GetSourceProvider(),
                TestUtils.GetSolutionManager(isSolutionOpen: isSolutionOpen),
                packageManagerFactory.Object,
                new Mock<IPackageRepository>().Object,
                null);
        }

        private static IPackageRepositoryFactory GetRepositoryFactory(IEnumerable<IPackage> packages = null) {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var repository = new Mock<IPackageRepository>();
            packages = packages ?? new[] { PackageUtility.CreatePackage("P1", "1.4"), PackageUtility.CreatePackage("P6") };
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
            return new VsPackageManager(TestUtils.GetSolutionManager(), remoteRepo.Object, fileSystem.Object, localRepo.Object, new Mock<IRecentPackageRepository>().Object);
        }

        private static IPackageSourceProvider GetSourceProvider() {
            Mock<IPackageSourceProvider> sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource("foo", "foo"));
            return sourceProvider.Object;
        }
    }
}