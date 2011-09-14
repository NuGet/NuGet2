using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;

namespace NuGet.VisualStudio.Test {

    using PackageUtility = NuGet.Test.PackageUtility;

    
    public class RecentPackageRepositoryTest {

        [Fact]
        public void RemovePackageMethodThrow() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act & Assert
            ExceptionAssert.Throws<NotSupportedException>(() => repository.RemovePackage(null));
        }

        [Fact]
        public void TestGetPackagesReturnNoPackageIfThereIsNoPackageMetadata() {
            // Arrange
            var repository = CreateRecentPackageRepository(null, new IPersistencePackageMetadata[0]);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }

        [Fact]
        public void TestGetPackagesReturnCorrectNumberOfPackages() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A
            // Calling GetPackages() should return package A.

            // Arrange
            var repository = CreateRecentPackageRepository(null, new[] { new PersistencePackageMetadata("A", "1.0") });

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal(new Version("1.0"), packages[0].Version);
        }

        [Fact]
        public void TestGetPackagesReturnNothingAfterCallingClear() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            repository.Clear();

            // Assert
            var packages = repository.GetPackages();
            Assert.False(packages.Any());
        }

        [Fact]
        public void TestGetPackagesReturnPackagesSortedByDateByDefault() {
            // Scenario: The remote repository contains package A, B and C
            // Recent settings store contains metadata for A and B
            // Calling GetPackages() should return package A and B, sorted by date (B goes before A)

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "2.0");

            var packagesList = new[] { packageA, packageB, packageC };
            var repository = CreateRecentPackageRepository(packagesList);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "B", "2.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [Fact]
        public void TestGetPackagesReturnCorrectNumberOfPackagesAfterAddingPackage() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A and B
            // Calling AddPackage(packageC)
            // Now GetPackages() should return A and C

            // Arrange
            var repository = CreateRecentPackageRepository();
            var packageC = PackageUtility.CreatePackage("C", "2.0");
            var recentPackageC = packageC;

            repository.AddPackage(recentPackageC);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "C", "2.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [Fact]
        public void TestGetPackagesReturnCorrectNumberOfPackagesAfterAddingPackageThatAlreadyExists() {
            // Scenario: The remote repository contains package A and B
            // Recent settings store contains metadata for A and B
            // Calling AddPackage(packageA)
            // Now GetPackages() should return A and B in that order

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");

            var packagesList = new[] { packageA, packageB };

            var repository = CreateRecentPackageRepository(packagesList: packagesList);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "B", "2.0");
            AssertPackage(packages[1], "A", "1.0");

            // Act
            var newPackage = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(newPackage);

            // Assert
            packages = repository.GetPackages().ToList();
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.0");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesReturnCorrectPackagesAfterAddingManyPackages() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var package1 = PackageUtility.CreatePackage("B", "1.0");
            var package2 = PackageUtility.CreatePackage("A", "1.0");
            var package3 = PackageUtility.CreatePackage("C", "2.0");
            var package4 = PackageUtility.CreatePackage("B", "2.0");
            var package5 = PackageUtility.CreatePackage("A", "1.0");
            var package6 = PackageUtility.CreatePackage("C", "2.0");

            // Act
            repository.AddPackage(package1);
            repository.AddPackage(package2);
            repository.AddPackage(package3);
            repository.AddPackage(package4);
            repository.AddPackage(package5);
            repository.AddPackage(package6);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.Equal(3, packages.Count);

            AssertPackage(packages[0], "C", "2.0");
            AssertPackage(packages[1], "A", "1.0");
            AssertPackage(packages[2], "B", "2.0");
        }

        [Fact]
        public void RecentPackageRepositoryStoresLatestPackageVersions() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var packageA1 = PackageUtility.CreatePackage("A", "1.0");
            var packageB1 = PackageUtility.CreatePackage("B", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0");
            var packageB2 = PackageUtility.CreatePackage("B", "2.0");
            var packageB3 = PackageUtility.CreatePackage("B", "3.0");

            // Act
            repository.AddPackage(packageA1);
            repository.AddPackage(packageB1);
            repository.AddPackage(packageB3);
            repository.AddPackage(packageA2);
            repository.AddPackage(packageB2);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.Equal(2, packages.Count);

            AssertPackage(packages[0], "B", "3.0");
            AssertPackage(packages[1], "A", "2.0");
        }

        [Fact]
        public void RecentPackageRepositoryUsesLatestVersionFromStore() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var packageA1 = PackageUtility.CreatePackage("A", "0.5");
            var packageB1 = PackageUtility.CreatePackage("B", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "0.6");
            var packageB2 = PackageUtility.CreatePackage("B", "2.0");
            var packageB3 = PackageUtility.CreatePackage("B", "3.0");


            // Act
            repository.AddPackage(packageA1);
            repository.AddPackage(packageB1);
            repository.AddPackage(packageB3);
            repository.AddPackage(packageA2);
            repository.AddPackage(packageB2);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.Equal(2, packages.Count);

            AssertPackage(packages[0], "B", "3.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [Fact]
        public void RecentPackageRepositoryCollapsesVersionsInStore() {
            // Arrange
            var storePackages = new[] {
                new PersistencePackageMetadata("A", "1.0", new DateTime(2037, 01, 01)),
                new PersistencePackageMetadata("C", "2.0", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("A", "2.5", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("C", "1.7", new DateTime(2010, 01, 01)),
                new PersistencePackageMetadata("C", "1.9", new DateTime(2011, 02, 01)),
            };

            var remotePackages = storePackages.Select(c => PackageUtility.CreatePackage(c.Id, c.Version.ToString()));
            var repository = CreateRecentPackageRepository(packagesList: remotePackages, settingsMetadata: storePackages);

            // Act and Assert
            var packages = repository.GetPackages().OfType<RecentPackage>().ToList();
            Assert.Equal(2, packages.Count);

            AssertPackage(packages[0], "A", "2.5");
            Assert.Equal(packages[0].LastUsedDate, new DateTime(2037, 01, 01));
            AssertPackage(packages[1], "C", "2.0");
            Assert.Equal(packages[1].LastUsedDate, new DateTime(2011, 02, 01));
        }

        [Fact]
        public void CallingClearMethodClearsAllPackagesFromSettingsStore() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            repository.Clear();
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }


        [Fact]
        public void RecentPackageRepositoryDoesNotReturnPackagesFromSourcesThatAreRemoved() {
            // Arrange
            var sources = new List<PackageSource> { new PackageSource("Source1"), new PackageSource("Source2") };
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.IsAny<string>())).Returns<string>(c => {
                switch (c) {
                    case "Source1":
                        var repo1 = new MockPackageRepository();
                        repo1.AddRange(new[] { PackageUtility.CreatePackage("Pack1") });
                        return repo1;
                    case "Source2":
                        var repo2 = new MockPackageRepository();
                        repo2.AddRange(new[] { PackageUtility.CreatePackage("Pack2", "1.1") });
                        return repo2;
                }
                return null;
            });

            var settingsManager = new MockSettingsManager();
            settingsManager.SavePackageMetadata(new[] {
                new PersistencePackageMetadata("Pack1", "1.0", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("Pack2", "1.1", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("Pack3", "1.0", new DateTime(2011, 01, 01)),
            });

            var sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var repository = new RecentPackageRepository(
                null, 
                factory.Object, 
                sourceProvider.Object, 
                settingsManager,
                new MockPackageRepository());

            // Act - 1, Scene - 1
            var packages = repository.GetPackages();

            // Assert 
            Assert.Equal(2, packages.Count());
            AssertPackage(packages.First(), "Pack1", "1.0");
            AssertPackage(packages.Last(), "Pack2", "1.1");

            // Act - 1, Scene - 2
            sources.Remove(sources.Last());

            packages = repository.GetPackages();
            Assert.Equal(1, packages.Count());
            AssertPackage(packages.First(), "Pack1", "1.0");

            // Fin
        }

        [Fact]
        public void RecentPackageRepositoryDoesNotReturnPackagesFromSourcesThatAreDisabled() {
            // Arrange
            var sources = new List<PackageSource> { new PackageSource("Source1"), new PackageSource("Source2", "Source2", isEnabled: false) };
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.IsAny<string>())).Returns<string>(c => {
                switch (c) {
                    case "Source1":
                        var repo1 = new MockPackageRepository();
                        repo1.AddRange(new[] { PackageUtility.CreatePackage("Pack1") });
                        return repo1;
                    case "Source2":
                        var repo2 = new MockPackageRepository();
                        repo2.AddRange(new[] { PackageUtility.CreatePackage("Pack2", "1.1") });
                        return repo2;
                }
                return null;
            });

            var settingsManager = new MockSettingsManager();
            settingsManager.SavePackageMetadata(new[] {
                new PersistencePackageMetadata("Pack1", "1.0", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("Pack2", "1.1", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("Pack3", "1.0", new DateTime(2011, 01, 01)),
            });

            var sourceProvider = new Mock<IPackageSourceProvider>();
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var repository = new RecentPackageRepository(
                null,
                factory.Object,
                sourceProvider.Object,
                settingsManager,
                new MockPackageRepository());

            // Act
            var packages = repository.GetPackages();

            // Assert 
            Assert.Equal(1, packages.Count());
            AssertPackage(packages.First(), "Pack1", "1.0");
            //AssertPackage(packages.Last(), "Pack2", "1.1");
        }

        private RecentPackageRepository CreateRecentPackageRepository(
            IEnumerable<IPackage> packagesList = null, 
            IEnumerable<IPersistencePackageMetadata> settingsMetadata = null,
            IPackageRepository cacheRepository = null) {
            if (packagesList == null) {
                var packageA = PackageUtility.CreatePackage("A", "1.0");
                var packageC = PackageUtility.CreatePackage("C", "2.0");

                packagesList = new[] { packageA, packageC };
            }

            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p => p.GetPackages()).Returns(packagesList.AsQueryable());

            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            mockRepositoryFactory.Setup(f => f.CreateRepository(It.IsAny<string>())).Returns(mockRepository.Object);

            var mockSettingsManager = new MockSettingsManager();

            if (settingsMetadata == null) {
                var A = new PersistencePackageMetadata("A", "1.0", new DateTime(2010, 8, 12));
                var B = new PersistencePackageMetadata("B", "2.0", new DateTime(2011, 3, 2));
                settingsMetadata = new[] { A, B };
            }

            mockSettingsManager.SavePackageMetadata(settingsMetadata);

            var mockPackageSourceProvider = new MockPackageSourceProvider();
            mockPackageSourceProvider.SavePackageSources(new[] { new PackageSource("source") });

            if (cacheRepository == null) {
                cacheRepository = new MockPackageRepository();
            }
            return new RecentPackageRepository(
                /* dte */ null, 
                mockRepositoryFactory.Object, 
                mockPackageSourceProvider, 
                mockSettingsManager,
                cacheRepository);
        }

        private void AssertPackage(IPackage package, string expectedId, string expectedVersion) {
            Assert.Equal(expectedId, package.Id);
            Assert.Equal(new Version(expectedVersion), package.Version);
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