using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
            var packages = localRepo.GetUpdates(remoteRepo);

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
            var packages = localRepo.GetUpdates(remoteRepo);

            // Assert
            Assert.IsFalse(packages.Any());
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

        private static IPackage CreateMockPackage(string name, string version, string desc = null) {
            Mock<IPackage> package = new Mock<IPackage>();
            package.SetupGet(p => p.Id).Returns(name);
            package.SetupGet(p => p.Version).Returns(Version.Parse(version));
            package.SetupGet(p => p.Description).Returns(desc);
            return package.Object;
        }
    }
}
