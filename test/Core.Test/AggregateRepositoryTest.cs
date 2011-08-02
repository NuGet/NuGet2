using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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

        [TestMethod]
        public void SupressErrorWorksForGetPackagesForRepositoriesThatThrowWhenInvoked() {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Throws(new InvalidOperationException()).Verifiable();

            var repository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("A"), 
                }, 
                mockRepository.Object,
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("B"), 
                } 
            });
            repository.IgnoreFailingRepositories = true;

            // Act
            var packages = repository.GetPackages().OrderBy(p => p.Id).ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
        }

        [TestMethod]
        public void SupressErrorWorksForFindPackagesForRepositoriesThatThrowWhenInvoked() {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Throws(new InvalidOperationException()).Verifiable();

            var packageLookup = new Mock<PackageLookupBase>();
            packageLookup.Setup(c => c.FindPackage(It.IsAny<string>(), It.IsAny<Version>())).Throws(new Exception());
            var mockRepositoryWithLookup = packageLookup.As<IPackageRepository>();
            mockRepositoryWithLookup.Setup(c => c.GetPackages()).Throws(new InvalidOperationException());

            var repository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("A"), 
                }, 
                mockRepository.Object,
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("B"), 
                },
                mockRepositoryWithLookup.Object
            });
            repository.IgnoreFailingRepositories = true;

            // Act
            var package = repository.FindPackage("C", new Version("1.0"));

            // Assert
            Assert.IsNull(package);
        }

        [TestMethod]
        public void SupressErrorWorksForGetDependenciesForRepositoriesThatThrowWhenInvoked() {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Throws(new InvalidOperationException()).Verifiable();
            var mockRepoWithLookup = new Mock<IPackageRepository>();
            mockRepository.As<IDependencyResolver>().Setup(c => c.ResolveDependency(It.IsAny<PackageDependency>(), It.IsAny<IPackageConstraintProvider>()));

            var repository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("A"), 
                }, 
                mockRepository.Object,
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("B"), 
                },
                mockRepoWithLookup.Object
            });
            repository.IgnoreFailingRepositories = true;

            // Act
            var package = repository.ResolveDependency(new PackageDependency("C"), null);

            // Assert
            Assert.IsNull(package);
        }

        [TestMethod]
        public void SupressErrorWorksForGetPackagesForRepositoriesThatThrowDuringEnumeration() {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable());

            var repository = new AggregateRepository(new[] { 
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("A"), 
                }, 
                mockRepository.Object,
                new MockPackageRepository { 
                    PackageUtility.CreatePackage("B"), 
                },
            });
            repository.IgnoreFailingRepositories = true;

            // Act
            var packages = repository.GetPackages();

            // Assert
            Assert.AreEqual(2, packages.Count());
        }

        [TestMethod]
        public void RepositoriesPropertyThrowsIfIgnoreFlagIsNotSet() {
            // Arrange
            var repositories = Enumerable.Range(0, 3).Select(e => {
                if (e == 1) {
                    throw new InvalidOperationException("Repository exception");
                }
                return new MockPackageRepository();
            });

            var aggregateRepository = new AggregateRepository(repositories);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => aggregateRepository.Repositories.Select(c => c.Source).ToList(), "Repository exception");
        }

        [TestMethod]
        public void GetPackagesSupressesExceptionForConsecutiveCalls() {
            // Arrange
            var repo1 = new Mock<IPackageRepository>();
            repo1.Setup(r => r.GetPackages()).Returns(Enumerable.Repeat(PackageUtility.CreatePackage("Foo"), 50).AsQueryable()).Verifiable();
            var repo2 = new Mock<IPackageRepository>();
            repo2.Setup(r => r.GetPackages()).Throws(new Exception()).Verifiable();

            var aggregateRepository = new AggregateRepository(new[] { repo1.Object, repo2.Object }) { IgnoreFailingRepositories = true };

            // Act 
            for (int i = 0; i < 5; i++) {
                aggregateRepository.GetPackages();
            }

            // Assert
            repo1.Verify(r => r.GetPackages(), Times.Exactly(5));
            repo2.Verify(r => r.GetPackages(), Times.AtMostOnce());
        }

        private static IEnumerable<IPackage> GetPackagesWithException() {
            yield return PackageUtility.CreatePackage("A");
            throw new InvalidOperationException();
        }

        public abstract class PackageLookupBase : IPackageLookup {
            public virtual IPackage FindPackage(string packageId, Version version) {
                throw new NotImplementedException();
            }
        }
    }
}
