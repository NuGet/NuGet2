using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;

namespace NuGet.VisualStudio.Test {

    using PackageUtility = NuGet.Test.PackageUtility;
    

    [TestClass]
    public class RecentPackageRepositoryTest {

        [TestMethod]
        public void RemovePackageMethodThrow() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act & Assert
            ExceptionAssert.Throws<NotSupportedException>(() => repository.RemovePackage(null));
        }

        [TestMethod]
        public void TestGetPackagesReturnNoPackageIfThereIsNoPackageMetadata() {
            // Arrange
            var repository = CreateRecentPackageRepository(empty: true);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        [TestMethod]
        public void TestGetPackagesReturnCorrectNumberOfPackages() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A and B
            // Calling GetPackages() should return package A.

            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual(new Version("1.0"), packages[0].Version);
        }

        [TestMethod]
        public void TestGetPackagesReturnCorrectNumberOfPackagesAfterAddingPackage() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A and B
            // Calling AddPackage(packageC)
            // Now GetPackages() should return A and C

            // Arrange
            var repository = CreateRecentPackageRepository();
            var packageC = PackageUtility.CreatePackage("C", "2.0");
            var recentPackageC = new RecentPackage(packageC, "foo.com");

            repository.AddPackage(recentPackageC);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("A", packages[1].Id);
            Assert.AreEqual(new Version("1.0"), packages[1].Version);

            Assert.AreEqual("C", packages[0].Id);
            Assert.AreEqual(new Version("2.0"), packages[0].Version);
        }

        [TestMethod]
        public void CallingClearMethodClearsAllPackagesFromSettingsStore() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            repository.Clear();
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        private RecentPackagesRepository CreateRecentPackageRepository(bool empty = false) {
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("C", "2.0");

            var packagesList = new IPackage[] { packageA, packageB };

            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p => p.GetPackages()).Returns(packagesList.AsQueryable());

            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            mockRepositoryFactory.Setup(f => f.CreateRepository(It.IsAny<PackageSource>())).Returns(mockRepository.Object);

            var mockSettingsManager = new MockSettingsManager();

            if (!empty) {
                var A = new PersistencePackageMetadata("A", "1.0", "bing.com");
                var B = new PersistencePackageMetadata("B", "2.0", "live.com");

                mockSettingsManager.SavePackageMetadata(new IPersistencePackageMetadata[] { A, B });
            }

            return new RecentPackagesRepository(null, mockRepositoryFactory.Object, mockSettingsManager);
        }

        private class PersistencePackageMetadata : IPersistencePackageMetadata {

            public PersistencePackageMetadata(string id, string version, string source) {
                Id = id;
                Version = new Version(version);
                Source = source;
            }

            public string Id { get; private set; }
            public Version Version { get; private set; }
            public string Source { get; private set; }
        }

        private class MockSettingsManager : IPersistencePackageSettingsManager {

            List<IPersistencePackageMetadata> _items = new List<IPersistencePackageMetadata>();

            public System.Collections.Generic.IEnumerable<IPersistencePackageMetadata> LoadPackageMetadata(int maximumCount) {
                return _items.Take(maximumCount);
            }

            public void SavePackageMetadata(System.Collections.Generic.IEnumerable<IPersistencePackageMetadata> packageMetadata) {
                _items.Clear();
                _items.AddRange(packageMetadata);
            }

            public void ClearPackageMetadata() {
                _items.Clear();
            }
        }
    }
}
