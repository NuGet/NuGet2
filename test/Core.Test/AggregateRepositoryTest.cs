using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class AggregateRepositoryTest
    {
        [Fact]
        public void GetPackagesNoOrderByReadsDistinctPackages()
        {
            // Arrange
            IPackage a = PackageUtility.CreatePackage("A"),
                     b = PackageUtility.CreatePackage("B"),
                     c = PackageUtility.CreatePackage("C");
            var repository = new AggregateRepository(new[] {
                new MockPackageRepository { c, a },
                new MockPackageRepository { b, a }
            });

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(new[] { c, a, b }, packages);
        }

        [Fact]
        public void GetPackagesRemoveDuplicates()
        {
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
            Assert.Equal(6, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal("B", packages[1].Id);
            Assert.Equal("C", packages[2].Id);
            Assert.Equal("D", packages[3].Id);
            Assert.Equal("E", packages[4].Id);
            Assert.Equal("F", packages[5].Id);
        }

        [Fact]
        public void GetPackagesWithPagingTakesLowestNElements()
        {
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
            Assert.Equal(5, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal("B", packages[1].Id);
            Assert.Equal("C", packages[2].Id);
            Assert.Equal("D", packages[3].Id);
            Assert.Equal("E", packages[4].Id);
        }

        [Fact]
        public void GetPackagesRemoveDuplicatesIfTheyAreTheSameVersion()
        {
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
            Assert.Equal(4, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Version);
            Assert.Equal("A", packages[1].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[1].Version);
            Assert.Equal("A", packages[2].Id);
            Assert.Equal(new SemanticVersion("3.0"), packages[2].Version);
            Assert.Equal("B", packages[3].Id);
        }

        [Fact]
        public void GetPackagesComplexOrderByAndDuplicatesRemovesDuplicatesAndMaintainsOrder()
        {
            // Arrange
            var r1 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", "2.0"),
            };

            var r2 = new MockPackageRepository() {
                PackageUtility.CreatePackage("A", downloadCount : 3),
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
            var packages = repository.GetPackages().OrderByDescending(p => p.DownloadCount)
                                                   .ThenBy(p => p.Id)
                                                   .ToList();

            // Assert
            Assert.Equal(4, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);
            Assert.Equal("A", packages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[1].Version);
            Assert.Equal("A", packages[2].Id);
            Assert.Equal(new SemanticVersion("3.0"), packages[2].Version);
            Assert.Equal("B", packages[3].Id);
        }

        [Fact]
        public void GetUpdates()
        {
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
            var updates = repository.GetUpdates(new[] { PackageUtility.CreatePackage("A", "1.0") }, includePrerelease: false, includeAllVersions: false).ToList();

            // Assert
            Assert.Equal(1, updates.Count);
            Assert.Equal("A", updates[0].Id);
            Assert.Equal(new SemanticVersion("3.0"), updates[0].Version);
        }

        [Fact]
        public void SupressErrorWorksForGetPackagesForRepositoriesThatThrowWhenInvoked()
        {
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
            Assert.Equal(2, packages.Count);
        }

        [Fact]
        public void SupressErrorWorksForFindPackagesForRepositoriesThatThrowWhenInvoked()
        {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Throws(new InvalidOperationException()).Verifiable();

            var packageLookup = new Mock<IPackageLookup>(MockBehavior.Strict);
            packageLookup.Setup(c => c.FindPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>())).Throws(new Exception());
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
            var package = repository.FindPackage("C", new SemanticVersion("1.0"));

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void SupressErrorWorksForGetDependenciesForRepositoriesThatThrowWhenInvoked()
        {
            // Arrange
            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(c => c.GetPackages()).Throws(new InvalidOperationException()).Verifiable();
            var mockRepoWithLookup = new Mock<IPackageRepository>();
            mockRepository.As<IDependencyResolver>().Setup(c => c.ResolveDependency(It.IsAny<PackageDependency>(), It.IsAny<IPackageConstraintProvider>(), false, It.IsAny<bool>(), DependencyVersion.Lowest));

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
            var package = DependencyResolveUtility.ResolveDependency(
                repository, 
                new PackageDependency("C"), 
                null, 
                allowPrereleaseVersions: false, 
                preferListedPackages: false,
                dependencyVersion: DependencyVersion.Lowest);

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void SupressErrorWorksForGetPackagesForRepositoriesThatThrowDuringEnumeration()
        {
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
            Assert.Equal(2, packages.Count());
        }

        [Fact]
        public void RepositoriesPropertyThrowsIfIgnoreFlagIsNotSet()
        {
            // Arrange
            var repositories = Enumerable.Range(0, 3).Select(e =>
            {
                if (e == 1)
                {
                    throw new InvalidOperationException("Repository exception");
                }
                return new MockPackageRepository();
            });

            var aggregateRepository = new AggregateRepository(repositories);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => aggregateRepository.Repositories.Select(c => c.Source).ToList(), "Repository exception");
        }

        [Fact]
        public void GetPackagesSupressesExceptionForConsecutiveCalls()
        {
            // Arrange
            var repo1 = new Mock<IPackageRepository>();
            repo1.Setup(r => r.GetPackages()).Returns(Enumerable.Repeat(PackageUtility.CreatePackage("Foo"), 50).AsQueryable()).Verifiable();
            var repo2 = new Mock<IPackageRepository>();
            repo2.Setup(r => r.GetPackages()).Throws(new Exception()).Verifiable();

            var aggregateRepository = new AggregateRepository(new[] { repo1.Object, repo2.Object }) { IgnoreFailingRepositories = true };

            // Act
            for (int i = 0; i < 5; i++)
            {
                aggregateRepository.GetPackages();
            }

            // Assert
            repo1.Verify(r => r.GetPackages(), Times.Exactly(5));
            repo2.Verify(r => r.GetPackages(), Times.AtMostOnce());
        }

        [Fact]
        public void ExistsReturnsTrueIfAnyRepositoryContainsPackage()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("Abc");
            var repo1 = new MockPackageRepository();
            var repo2 = new MockPackageRepository();
            repo2.Add(package);

            var aggregateRepository = new AggregateRepository(new[] { repo1, repo2 });

            // Act
            var exists = aggregateRepository.Exists("Abc", new SemanticVersion("1.0"));

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void GetUpdatesReturnsAggregateOfUpdates()
        {
            // Arrange
            var package_10 = PackageUtility.CreatePackage("A", "1.0");
            var package_11 = PackageUtility.CreatePackage("A", "1.1");
            var package_12 = PackageUtility.CreatePackage("A", "1.2");

            var repo1 = new MockPackageRepository();
            repo1.Add(package_11);
            var repo2 = new MockPackageRepository();
            repo2.Add(package_12);

            var aggregateRepository = new AggregateRepository(new[] { repo1, repo2 });

            // Act
            var updates = aggregateRepository.GetUpdates(new[] { package_10 }, false, includeAllVersions: true);

            // Assert
            Assert.Equal(2, updates.Count());
            Assert.Same(package_11, updates.ElementAt(0));
            Assert.Same(package_12, updates.ElementAt(1));
        }

        [Fact]
        public void GetUpdatesReturnsDistinctSetOfPackages()
        {
            // Arrange
            var package_10 = PackageUtility.CreatePackage("A", "1.0");
            var package_11 = PackageUtility.CreatePackage("A", "1.1");
            var package_12 = PackageUtility.CreatePackage("A", "1.2");

            var repo1 = new MockPackageRepository();
            repo1.Add(package_12);
            repo1.Add(package_11);
            var repo2 = new MockPackageRepository();
            repo2.Add(package_12);

            var aggregateRepository = new AggregateRepository(new[] { repo1, repo2 });

            // Act
            var updates = aggregateRepository.GetUpdates(new[] { package_10 }, includePrerelease: false, includeAllVersions: true);

            // Assert
            Assert.Equal(2, updates.Count());
            Assert.Same(package_11, updates.ElementAt(0));
            Assert.Same(package_12, updates.ElementAt(1));
        }

        private static IEnumerable<IPackage> GetPackagesWithException()
        {
            yield return PackageUtility.CreatePackage("A");
            throw new InvalidOperationException();
        }
    }
}