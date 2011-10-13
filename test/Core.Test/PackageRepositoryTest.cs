using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class PackageRepositoryTest
    {
        [Fact]
        public void FindByIdReturnsPackage()
        {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package = repo.FindPackage(packageId: "A");

            // Assert
            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
        }

        [Fact]
        public void FindByIdReturnsNullWhenPackageNotFound()
        {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package = repo.FindPackage(packageId: "X");

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void FindByIdAndVersionReturnsPackage()
        {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage(packageId: "A", version: SemanticVersion.Parse("1.0"));

            // Assert
            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
            Assert.Equal(SemanticVersion.Parse("1.0"), package.Version);
        }

        [Fact]
        public void FindByIdAndVersionReturnsNullWhenPackageNotFound()
        {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var package1 = repo.FindPackage(packageId: "X", version: SemanticVersion.Parse("1.0"));
            var package2 = repo.FindPackage(packageId: "A", version: SemanticVersion.Parse("1.1"));

            // Assert
            Assert.Null(package1 ?? package2);
        }

        [Fact]
        public void FindByIdAndVersionRangeReturnsPackage()
        {
            // Arrange
            var repo = GetRemoteRepository();
            var versionSpec = VersionUtility.ParseVersionSpec("[0.9, 1.1]");

            // Act
            var package = repo.FindPackage("A", versionSpec, allowPrereleaseVersions: false);

            // Assert
            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
            Assert.Equal(SemanticVersion.Parse("1.0"), package.Version);
        }

        [Fact]
        public void FindByIdAndVersionRangeReturnsNullWhenPackageNotFound()
        {
            // Arrange
            var repo = GetLocalRepository();
            var versionSpec = VersionUtility.ParseVersionSpec("[0.9, 1.1]");

            // Act
            var package1 = repo.FindPackage("X", VersionUtility.ParseVersionSpec("[0.9, 1.1]"), allowPrereleaseVersions: false);
            var package2 = repo.FindPackage("A", VersionUtility.ParseVersionSpec("[1.4, 1.5]"), allowPrereleaseVersions: false);

            // Assert
            Assert.Null(package1 ?? package2);
        }

        [Fact]
        public void FindPackageByIdVersionAndVersionRangesUsesRangeIfExactVersionIsNull()
        {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage("A", VersionUtility.ParseVersionSpec("[0.6, 1.1.5]"), allowPrereleaseVersions: false);

            // Assert
            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
            Assert.Equal(SemanticVersion.Parse("1.0"), package.Version);
        }

        [Fact]
        public void FindPackagesReturnsPackagesWithTermInPackageTagOrDescriptionOrId()
        {
            // Arrange
            var term = "TAG";
            var repo = new MockPackageRepository();
            repo.Add(CreateMockPackage("A", "1.0", "Description", " TAG "));
            repo.Add(CreateMockPackage("B", "2.0", "Description", "Tags"));
            repo.Add(CreateMockPackage("C", "1.0", "This description has tags in it"));
            repo.Add(CreateMockPackage("D", "1.0", "Description"));
            repo.Add(CreateMockPackage("TagCloud", "1.0", "Description"));

            // Act
            var packages = repo.GetPackages().Find(term).ToList();

            // Assert
            Assert.Equal(3, packages.Count);
            Assert.Equal("A", packages[0].Id);
            Assert.Equal("C", packages[1].Id);
            Assert.Equal("TagCloud", packages[2].Id);
        }

        [Fact]
        public void FindPackagesReturnsPackagesWithTerm()
        {
            // Arrange
            var term = "B xaml";
            var repo = GetRemoteRepository();

            // Act
            var packages = repo.GetPackages().Find(term);

            // Assert
            Assert.Equal(packages.Count(), 2);
            packages = packages.OrderBy(p => p.Id);
            Assert.Equal(packages.ElementAt(0).Id, "B");
            Assert.Equal(packages.ElementAt(1).Id, "C");
        }

        [Fact]
        public void FindPackagesReturnsEmptyCollectionWhenNoPackageContainsTerm()
        {
            // Arrange
            var term = "does-not-exist";
            var repo = GetRemoteRepository();

            // Act
            var packages = repo.GetPackages().Find(term);

            // Assert
            Assert.False(packages.Any());
        }

        [Fact]
        public void FindPackagesReturnsAllPackagesWhenSearchTermIsNullOrEmpty()
        {
            // Arrange
            var repo = GetLocalRepository();

            // Act
            var packages1 = repo.GetPackages().Find(String.Empty);
            var packages2 = repo.GetPackages().Find(null);
            var packages3 = repo.GetPackages();

            // Assert
            Assert.Equal(packages1.ToList(), packages2.ToList());
            Assert.Equal(packages2.ToList(), packages3.ToList());
        }

        [Fact]
        public void SearchUsesInterfaceIfImplementedByRepository()
        {
            // Arrange
            var repo = new Mock<MockPackageRepository>(MockBehavior.Strict);
            repo.Setup(m => m.GetPackages()).Returns(Enumerable.Empty<IPackage>().AsQueryable());
            repo.As<ISearchableRepository>().Setup(m => m.Search(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                                            .Returns(new[] { PackageUtility.CreatePackage("A") }.AsQueryable());

            // Act
            var packages = repo.Object.Search("Hello", new[] { ".NETFramework" }).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("A", packages[0].Id);
        }

        [Fact]
        public void GetUpdatesReturnsPackagesWithUpdates()
        {
            // Arrange 
            var localRepo = GetLocalRepository();
            var remoteRepo = GetRemoteRepository();

            // Act
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages(), includePrerelease: false);

            // Assert
            Assert.True(packages.Any());
            Assert.Equal(packages.First().Id, "A");
            Assert.Equal(packages.First().Version, SemanticVersion.Parse("1.2"));
        }

        [Fact]
        public void GetUpdatesReturnsEmptyCollectionWhenSourceRepositoryIsEmpty()
        {
            // Arrange 
            var localRepo = GetLocalRepository();
            var remoteRepo = GetEmptyRepository();

            // Act
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages(), includePrerelease: false);

            // Assert
            Assert.False(packages.Any());
        }

        [Fact]
        public void FindDependencyPicksHighestVersionIfNotSpecified()
        {
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
            IPackage package = repository.ResolveDependency(dependency, allowPrereleaseVersions: false);

            // Assert
            Assert.Equal("B", package.Id);
            Assert.Equal(new SemanticVersion("2.0"), package.Version);
        }

        [Fact]
        public void FindPackageNormalizesVersionBeforeComparing()
        {
            // Arrange
            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "1.0.0"),
                PackageUtility.CreatePackage("B", "1.0.0.1")
            };

            // Act
            IPackage package = repository.FindPackage("B", new SemanticVersion("1.0"));

            // Assert
            Assert.Equal("B", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0"), package.Version);
        }

        [Fact]
        public void FindDependencyPicksLowestMajorAndMinorVersionButHighestBuildAndRevision()
        {
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
            IPackage package1 = repository.ResolveDependency(dependency1, allowPrereleaseVersions: false);
            IPackage package2 = repository.ResolveDependency(dependency2, allowPrereleaseVersions: false);
            IPackage package3 = repository.ResolveDependency(dependency3, allowPrereleaseVersions: false);
            IPackage package4 = repository.ResolveDependency(dependency4, allowPrereleaseVersions: false);
            IPackage package5 = repository.ResolveDependency(dependency5, allowPrereleaseVersions: false);

            // Assert
            Assert.Equal("B", package1.Id);
            Assert.Equal(new SemanticVersion("1.0.9"), package1.Version);
            Assert.Equal("B", package2.Id);
            Assert.Equal(new SemanticVersion("1.0.9"), package2.Version);
            Assert.Equal("B", package3.Id);
            Assert.Equal(new SemanticVersion("1.0.9"), package3.Version);
            Assert.Equal("B", package4.Id);
            Assert.Equal(new SemanticVersion("1.0"), package4.Version);
            Assert.Equal("B", package5.Id);
            Assert.Equal(new SemanticVersion("1.0.1"), package5.Version);
        }

        [Fact]
        public void ResolveSafeVersionReturnsNullIfPackagesNull()
        {
            // Act
            var package = PackageRepositoryExtensions.ResolveSafeVersion(null);

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void ResolveSafeVersionReturnsNullIfEmptyPackages()
        {
            // Act
            var package = PackageRepositoryExtensions.ResolveSafeVersion(Enumerable.Empty<IPackage>());

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void ResolveSafeVersionReturnsHighestBuildAndRevisionWithLowestMajorAndMinor()
        {
            var packages = new[] { 
                PackageUtility.CreatePackage("A", "0.9"),
                PackageUtility.CreatePackage("A", "0.9.3"),
                PackageUtility.CreatePackage("A", "1.0"),
                PackageUtility.CreatePackage("A", "1.0.2"),
                PackageUtility.CreatePackage("A", "1.0.12"),
                PackageUtility.CreatePackage("A", "1.0.13"),
            };

            // Act
            var package = PackageRepositoryExtensions.ResolveSafeVersion(packages);

            // Assert
            Assert.NotNull(package);
            Assert.Equal("A", package.Id);
            Assert.Equal(new SemanticVersion("0.9.3"), package.Version);
        }

        private static IPackageRepository GetEmptyRepository()
        {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(() => Enumerable.Empty<IPackage>().AsQueryable());
            return repository.Object;
        }

        private static IPackageRepository GetRemoteRepository()
        {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            var packages = new[] { CreateMockPackage("A", "1.0", "scripts style"), 
                                   CreateMockPackage("B", "1.0", "testing"), 
                                   CreateMockPackage("C", "2.0", "xaml"), 
                                   CreateMockPackage("A", "1.2", "a updated desc") };
            repository.Setup(c => c.GetPackages()).Returns(() => packages.AsQueryable());
            return repository.Object;
        }

        private static IPackageRepository GetLocalRepository()
        {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            var packages = new[] { CreateMockPackage("A", "1.0"), CreateMockPackage("B", "1.0") };
            repository.Setup(c => c.GetPackages()).Returns(() => packages.AsQueryable());
            return repository.Object;
        }

        private static IPackage CreateMockPackage(string name, string version, string desc = null, string tags = null)
        {
            Mock<IPackage> package = new Mock<IPackage>();
            package.SetupGet(p => p.Id).Returns(name);
            package.SetupGet(p => p.Version).Returns(SemanticVersion.Parse(version));
            package.SetupGet(p => p.Description).Returns(desc);
            package.SetupGet(p => p.Tags).Returns(tags);
            return package.Object;
        }
    }
}
