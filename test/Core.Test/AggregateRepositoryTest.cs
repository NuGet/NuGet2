using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class AggregateRepositoryTest {
        [TestMethod]
        public void GetPackagesNoOrderByExpressionThrows() {
            // Arrange
            var repository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("A"), 
                }, 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("B"), 
                } 
            });

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => repository.GetPackages().ToList(), "Aggregate queries require at least one OrderBy.");
        }

        [TestMethod]
        public void GetPackagesRemoveDuplicates() {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("B"),
                PackageUtility.CreatePackage("C"),
                PackageUtility.CreatePackage("D"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("D"),
                PackageUtility.CreatePackage("E"),
                PackageUtility.CreatePackage("F"),
            };
            var repository = new AggregateRepository(new[] { r1, r2 });

            // Act
            var packages = repository.GetPackages().OrderBy(p => p.Id).ToList();

            // Assert
            Assert.AreEqual(6, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual("B", packages[1].Id);
            Assert.AreEqual("C", packages[2].Id);
            Assert.AreEqual("D", packages[3].Id);
            Assert.AreEqual("E", packages[4].Id);
            Assert.AreEqual("F", packages[5].Id);
        }

        [TestMethod]
        public void GetPackagesWithPagingTakesLowestNElements() {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("B"),
                PackageUtility.CreatePackage("E"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("C"),
                PackageUtility.CreatePackage("D"),
                PackageUtility.CreatePackage("F"),
            };
            var repository = new AggregateRepository(new[] { r1, r2 });

            // Act
            var packages = repository.GetPackages().OrderBy(p => p.Id).Take(5).ToList();

            // Assert
            Assert.AreEqual(5, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual("B", packages[1].Id);
            Assert.AreEqual("C", packages[2].Id);
            Assert.AreEqual("D", packages[3].Id);
            Assert.AreEqual("E", packages[4].Id);
        }


        [TestMethod]
        public void GetPackagesRemoveDuplicatesIfTheyAreTheSameVersion() {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "2.0"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
            };

            var r3 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "3.0"),
            };

            var r4 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("B"),
            };
            var repository = new AggregateRepository(new[] { r1, r2, r3, r4 });

            // Act
            var packages = repository.GetPackages().OrderBy(p => p.Id).ToList();

            // Assert
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual(new Version("2.0"), packages[0].Version);
            Assert.AreEqual("A", packages[1].Id);
            Assert.AreEqual(new Version("1.0"), packages[1].Version);
            Assert.AreEqual("A", packages[2].Id);
            Assert.AreEqual(new Version("3.0"), packages[2].Version);
            Assert.AreEqual("B", packages[3].Id);
        }

        [TestMethod]
        public void GetPackagesComplexOrderByAndDuplicatesRemovesDuplicatesAndMaintainsOrder() {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "2.0"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", rating : 3),
            };

            var r3 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "3.0"),
            };

            var r4 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("B"),
            };
            var repository = new AggregateRepository(new[] { r1, r2, r3, r4 });

            // Act
            var packages = repository.GetPackages().OrderByDescending(p => p.Rating)
                                                   .ThenBy(p => p.Id)
                                                   .ToList();

            // Assert
            Assert.AreEqual(4, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual(new Version("1.0"), packages[0].Version);
            Assert.AreEqual("A", packages[1].Id);
            Assert.AreEqual(new Version("2.0"), packages[1].Version);
            Assert.AreEqual("A", packages[2].Id);
            Assert.AreEqual(new Version("3.0"), packages[2].Version);
            Assert.AreEqual("B", packages[3].Id);
        }

        [TestMethod]
        public void GetUpdates() {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "2.0"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
            };

            var r3 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "3.0"),
            };

            var r4 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A"),
                PackageUtility.CreatePackage("B"),
            };

            var repository = new AggregateRepository(new[] { r1, r2, r3, r4 });

            // Act
            var updates = repository.GetUpdates(new[] { PackageUtility.CreatePackage("A", "1.0") }).ToList();

            // Assert
            Assert.AreEqual(1, updates.Count);
            Assert.AreEqual("A", updates[0].Id);
            Assert.AreEqual(new Version("3.0"), updates[0].Version);
        }
    }
}
