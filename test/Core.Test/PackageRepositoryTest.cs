using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class PackageRepositoryTest {
        [TestMethod]
        public void FindByIdReturnsPackage() {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package = repo.FindPackage(packageId: "A");

            // Assert
            Assert.IsNotNull(package);
            Assert.AreEqual("A", package.Id);
        }

        [TestMethod]
        public void FindByIdReturnsNullWhenPackageNotFound() {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package = repo.FindPackage(packageId: "X");

            // Assert
            Assert.IsNull(package);
        }

        [TestMethod]
        public void FindByIdAndVersionReturnsPackage() {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage(packageId: "A", version: Version.Parse("1.0"));

            // Assert
            Assert.IsNotNull(package);
            Assert.AreEqual("A", package.Id);
            Assert.AreEqual(Version.Parse("1.0"), package.Version);
        }

        [TestMethod]
        public void FindByIdAndVersionReturnsNullWhenPackageNotFound() {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package1 = repo.FindPackage(packageId: "X", version: Version.Parse("1.0"));
            var package2 = repo.FindPackage(packageId: "A", version: Version.Parse("1.1"));

            // Assert
            Assert.IsNull(package1 ?? package2);
        }

        [TestMethod]
        public void FindByIdAndVersionRangeReturnsPackage() {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage("A", "[0.9, 1.1]");

            // Assert
            Assert.IsNotNull(package);
            Assert.AreEqual("A", package.Id);
            Assert.AreEqual(Version.Parse("1.0"), package.Version);
        }

        [TestMethod]
        public void FindByIdAndVersionRangeReturnsNullWhenPackageNotFound() {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package1 = repo.FindPackage("X", "[0.9, 1.1]");
            var package2 = repo.FindPackage("A", "[1.4, 1.5]");

            // Assert
            Assert.IsNull(package1 ?? package2);
        }

        [TestMethod]
        public void FindPackageByIdVersionAndVersionRangesUsesRangeIfExactVersionIsNull() {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage("A", "[0.6, 1.1.5]");

            // Assert
            Assert.IsNotNull(package);
            Assert.AreEqual("A", package.Id);
            Assert.AreEqual(Version.Parse("1.0"), package.Version);
        }

        [TestMethod]
        public void FindPackagesReturnsPackagesWithTermInPackageTagOrDescriptionOrId() {
            // Arrange
            var term = "TAG";
            var repo = new MockPackageRepository();
            repo.Add(CreateMockPackage("A", "1.0", "Description", " TAG "));
            repo.Add(CreateMockPackage("B", "2.0", "Description", "Tags"));
            repo.Add(CreateMockPackage("C", "1.0", "This description has tags in it"));
            repo.Add(CreateMockPackage("D", "1.0", "Description"));
            repo.Add(CreateMockPackage("TagCloud", "1.0", "Description"));

            // Act
            var packages = repo.GetPackages().Find(term.Split()).ToList();

            // Assert
            Assert.AreEqual(3, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual("C", packages[1].Id);
            Assert.AreEqual("TagCloud", packages[2].Id);
        }

        [TestMethod]
        public void FindPackagesReturnsPackagesWithTerm() {
            // Arrange
            var term = "B xaml";
            var repo = GetRemoteRepository();

            // Act
            var packages = repo.GetPackages().Find(term.Split());

            // Assert
            Assert.AreEqual(packages.Count(), 2);
            packages = packages.OrderBy(p => p.Id);
            Assert.AreEqual(packages.ElementAt(0).Id, "B");
            Assert.AreEqual(packages.ElementAt(1).Id, "C");
        }

        [TestMethod]
        public void FindPackagesReturnsEmptyCollectionWhenNoPackageContainsTerm() {
            // Arrange
            var term = "does-not-exist";
            var repo = GetRemoteRepository();

            // Act
            var packages = repo.GetPackages().Find(term.Split());

            // Assert
            Assert.IsFalse(packages.Any());
        }

        [TestMethod]
        public void FindPackagesReturnsAllPackagesWhenSearchTermIsNullOrEmpty() {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var packages1 = repo.GetPackages().Find(String.Empty);
            var packages2 = repo.GetPackages().Find(null);
            var packages3 = repo.GetPackages();

            // Assert
            CollectionAssert.AreEqual(packages1.ToList(), packages2.ToList());
            CollectionAssert.AreEqual(packages2.ToList(), packages3.ToList());
        }

        [TestMethod]
        public void GetUpdatesReturnsPackagesWithUpdates() {
            // Arrange 
            var localRepo = GetLocalRepository();
            var remoteRepo = GetRemoteRepository();

            // Act
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages());

            // Assert
            Assert.IsTrue(packages.Any());
            Assert.AreEqual(packages.First().Id, "A");
            Assert.AreEqual(packages.First().Version, Version.Parse("1.2"));
        }

        [TestMethod]
        public void GetUpdatesReturnsEmptyCollectionWhenSourceRepositoryIsEmpty() {
            // Arrange 
            var localRepo = GetLocalRepository();
            var remoteRepo = GetEmptyRepository();

            // Act
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages());

            // Assert
            Assert.IsFalse(packages.Any());
        }

        [TestMethod]
        public void FindDependencyPicksHighestVersionIfNotSpecified() {
            // Arrange
            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "2.0"),
                PackageUtility.CreatePackage("B", "1.0"),
                PackageUtility.CreatePackage("B", "1.0.1"),
                PackageUtility.CreatePackage("B", "1.0.9"),
                PackageUtility.CreatePackage("B", "1.1")
            };

            var dependency = new PackageDependency("B");

            // Act
            IPackage package = repository.FindDependency(dependency);

            // Assert
            Assert.AreEqual("B", package.Id);
            Assert.AreEqual(new Version("2.0"), package.Version);
        }

        [TestMethod]
        public void FindDependencyPicksLowestMajorAndMinorVersionButHighestBuildAndRevision() {
            // Arrange
            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "2.0"),
                PackageUtility.CreatePackage("B", "1.0"),
                PackageUtility.CreatePackage("B", "1.0.1"),
                PackageUtility.CreatePackage("B", "1.0.9"),
                PackageUtility.CreatePackage("B", "1.1")
            };

            // B >= 1.0
            PackageDependency dependency1 = PackageDependency.CreateDependency("B", "1.0");

            // B >= 1.0.0
            PackageDependency dependency2 = PackageDependency.CreateDependency("B", "1.0.0");

            // B >= 1.0.0.0
            PackageDependency dependency3 = PackageDependency.CreateDependency("B", "1.0.0.0");

            // B = 1.0
            PackageDependency dependency4 = PackageDependency.CreateDependency("B", "[1.0]");

            // B >= 1.0.0 && <= 1.0.8
            PackageDependency dependency5 = PackageDependency.CreateDependency("B", "[1.0.0, 1.0.8]");

            // Act
            IPackage package1 = repository.FindDependency(dependency1);
            IPackage package2 = repository.FindDependency(dependency2);
            IPackage package3 = repository.FindDependency(dependency3);
            IPackage package4 = repository.FindDependency(dependency4);
            IPackage package5 = repository.FindDependency(dependency5);

            // Assert
            Assert.AreEqual("B", package1.Id);
            Assert.AreEqual(new Version("1.0.9"), package1.Version);
            Assert.AreEqual("B", package2.Id);
            Assert.AreEqual(new Version("1.0.9"), package2.Version);
            Assert.AreEqual("B", package3.Id);
            Assert.AreEqual(new Version("1.0.9"), package3.Version);
            Assert.AreEqual("B", package4.Id);
            Assert.AreEqual(new Version("1.0"), package4.Version);
            Assert.AreEqual("B", package5.Id);
            Assert.AreEqual(new Version("1.0.1"), package5.Version);
        }

        private static IPackageRepository GetEmptyRepository() {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(() => Enumerable.Empty<IPackage>().AsQueryable());
            return repository.Object;
        }

        private static IPackageRepository GetRemoteRepository() {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            var packages = new[] { CreateMockPackage("A", "1.0", "scripts style"), 
                                   CreateMockPackage("B", "1.0", "testing"), 
                                   CreateMockPackage("C", "2.0", "xaml"), 
                                   CreateMockPackage("A", "1.2", "a updated desc") };
            repository.Setup(c => c.GetPackages()).Returns(() => packages.AsQueryable());
            return repository.Object;
        }

        private static IPackageRepository GetLocalRepository() {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            var packages = new[] { CreateMockPackage("A", "1.0"), CreateMockPackage("B", "1.0") };
            repository.Setup(c => c.GetPackages()).Returns(() => packages.AsQueryable());
            return repository.Object;
        }

        private static IPackage CreateMockPackage(string name, string version, string desc = null, string tags = null) {
            Mock<IPackage> package = new Mock<IPackage>();
            package.SetupGet(p => p.Id).Returns(name);
            package.SetupGet(p => p.Version).Returns(Version.Parse(version));
            package.SetupGet(p => p.Description).Returns(desc);
            package.SetupGet(p => p.Tags).Returns(tags);
            return package.Object;
        }
    }
}
