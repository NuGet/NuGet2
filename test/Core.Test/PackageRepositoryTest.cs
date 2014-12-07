using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
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
            var package = repo.FindPackage("A", versionSpec, allowPrereleaseVersions: false, allowUnlisted: true);

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

            // Act
            var package1 = repo.FindPackage("X", VersionUtility.ParseVersionSpec("[0.9, 1.1]"), allowPrereleaseVersions: false, allowUnlisted: true);
            var package2 = repo.FindPackage("A", VersionUtility.ParseVersionSpec("[1.4, 1.5]"), allowPrereleaseVersions: false, allowUnlisted: true);

            // Assert
            Assert.Null(package1 ?? package2);
        }

        [Fact]
        public void FindPackageByIdVersionAndVersionRangesUsesRangeIfExactVersionIsNull()
        {
            // Arrange
            var repo = GetRemoteRepository();

            // Act
            var package = repo.FindPackage("A", VersionUtility.ParseVersionSpec("[0.6, 1.1.5]"), allowPrereleaseVersions: false, allowUnlisted: true);

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
        public void FindPackagesReturnsPrereleasePackagesIfTheFlagIsSetToTrue()
        {
            // Arrange
            var term = "B";
            var repo = GetRemoteRepository(includePrerelease: true);

            // Act
            var packages = repo.GetPackages().Find(term);

            // Assert
            Assert.Equal(packages.Count(), 2);
            packages = packages.OrderBy(p => p.Id);
            Assert.Equal(packages.ElementAt(0).Id, "B");
            Assert.Equal(packages.ElementAt(0).Version, new SemanticVersion("1.0"));
            Assert.Equal(packages.ElementAt(1).Id, "B");
            Assert.Equal(packages.ElementAt(1).Version, new SemanticVersion("1.0-beta"));
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
            repo.As<IServiceBasedRepository>().Setup(m => m.Search(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), false, false))
                                            .Returns(new[] { PackageUtility.CreatePackage("A") }.AsQueryable());

            // Act
            var packages = repo.Object.Search("Hello", new[] { ".NETFramework" }, allowPrereleaseVersions: false).ToList();

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
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages(), includePrerelease: false, includeAllVersions: false);

            // Assert
            Assert.True(packages.Any());
            Assert.Equal(packages.First().Id, "A");
            Assert.Equal(packages.First().Version, SemanticVersion.Parse("1.2"));
        }
        
        [Fact]
        public void GetUpdatesDoesNotInvokeServiceMethodIfLocalRepositoryDoesNotHaveAnyPackages()
        {
            // Arrange 
            var localRepo = new MockPackageRepository();
            var serviceRepository = new Mock<IServiceBasedRepository>(MockBehavior.Strict);
            var remoteRepo = serviceRepository.As<IPackageRepository>().Object;

            // Act
            remoteRepo.GetUpdates(localRepo.GetPackages(), includePrerelease: false, includeAllVersions: false);

            // Assert
            serviceRepository.Verify(s => s.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), false, false, It.IsAny<IEnumerable<FrameworkName>>(), It.IsAny<IEnumerable<IVersionSpec>>()), Times.Never());
        }

        [Fact]
        public void GetUpdatesReturnsEmptyCollectionWhenSourceRepositoryIsEmpty()
        {
            // Arrange 
            var localRepo = GetLocalRepository();
            var remoteRepo = GetEmptyRepository();

            // Act
            var packages = remoteRepo.GetUpdates(localRepo.GetPackages(), includePrerelease: false, includeAllVersions: false);

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
            IPackage package = repository.ResolveDependency(dependency, allowPrereleaseVersions: false, preferListedPackages: false);

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

        // Test that when dependencyVersion is DependencyVersions.Lowest, 
        // the dependency with the lowest patch number is picked.
        [Fact]
        public void FindDependencyPicksLowestMajorAndMinorVersion()
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
            IPackage package1 = repository.ResolveDependency(
                dependency1, constraintProvider: null, allowPrereleaseVersions: false, 
                preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);
            IPackage package2 = repository.ResolveDependency(
                dependency2, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);
            IPackage package3 = repository.ResolveDependency(
                dependency3, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);
            IPackage package4 = repository.ResolveDependency(
                dependency4, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);
            IPackage package5 = repository.ResolveDependency(
                dependency5, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Lowest);

            // Assert
            Assert.Equal("B", package1.Id);
            Assert.Equal(new SemanticVersion("1.0"), package1.Version);
            Assert.Equal("B", package2.Id);
            Assert.Equal(new SemanticVersion("1.0"), package2.Version);
            Assert.Equal("B", package3.Id);
            Assert.Equal(new SemanticVersion("1.0"), package3.Version);
            Assert.Equal("B", package4.Id);
            Assert.Equal(new SemanticVersion("1.0"), package4.Version);
            Assert.Equal("B", package5.Id);
            Assert.Equal(new SemanticVersion("1.0"), package5.Version);
        }

        // Test that when dependencyVersion is DependencyVersions.Highest, 
        // the dependency with the highest version is picked.
        [Fact]
        public void FindDependencyPicksHighest()
        {
            // Arrange
            var repository = new MockPackageRepository() {
                PackageUtility.CreatePackage("B", "3.0"),
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

            // B >= 1.0.0 && <= 2.0
            PackageDependency dependency5 = PackageDependency.CreateDependency("B", "[1.0.0, 2.0]");

            // Act
            IPackage package1 = repository.ResolveDependency(
                dependency1, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Highest);
            IPackage package2 = repository.ResolveDependency(
                dependency2, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Highest);
            IPackage package3 = repository.ResolveDependency(
                dependency3, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Highest);
            IPackage package4 = repository.ResolveDependency(
                dependency4, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Highest);
            IPackage package5 = repository.ResolveDependency(
                dependency5, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.Highest);

            // Assert
            Assert.Equal("B", package1.Id);
            Assert.Equal(new SemanticVersion("3.0"), package1.Version);
            Assert.Equal("B", package2.Id);
            Assert.Equal(new SemanticVersion("3.0"), package2.Version);
            Assert.Equal("B", package3.Id);
            Assert.Equal(new SemanticVersion("3.0"), package3.Version);
            Assert.Equal("B", package4.Id);
            Assert.Equal(new SemanticVersion("1.0"), package4.Version);
            Assert.Equal("B", package5.Id);
            Assert.Equal(new SemanticVersion("2.0"), package5.Version);
        }
        
        // Test that when dependencyVersion is DependencyVersions.HighestMinor, 
        // the dependency with the highest minor version is picked.
        [Fact]
        public void FindDependencyPicksHighestMinor()
        {
            // Arrange
            var repository = new MockPackageRepository() {                
                PackageUtility.CreatePackage("B", "1.0"),
                PackageUtility.CreatePackage("B", "1.0.1"),
                PackageUtility.CreatePackage("B", "1.0.9"),
                PackageUtility.CreatePackage("B", "1.1"),
                PackageUtility.CreatePackage("B", "2.0"),
                PackageUtility.CreatePackage("B", "3.0")
            };

            // B >= 1.0
            PackageDependency dependency1 = PackageDependency.CreateDependency("B", "1.0");

            // B >= 1.0.0
            PackageDependency dependency2 = PackageDependency.CreateDependency("B", "1.0.0");

            // B >= 1.0.0.0
            PackageDependency dependency3 = PackageDependency.CreateDependency("B", "1.0.0.0");

            // B = 1.0
            PackageDependency dependency4 = PackageDependency.CreateDependency("B", "[1.0]");

            // B >= 1.0.0 && <= 2.0
            PackageDependency dependency5 = PackageDependency.CreateDependency("B", "[1.0.0, 2.0]");

            // Act
            IPackage package1 = repository.ResolveDependency(
                dependency1, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.HighestMinor);
            IPackage package2 = repository.ResolveDependency(
                dependency2, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.HighestMinor);
            IPackage package3 = repository.ResolveDependency(
                dependency3, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.HighestMinor);
            IPackage package4 = repository.ResolveDependency(
                dependency4, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.HighestMinor);
            IPackage package5 = repository.ResolveDependency(
                dependency5, constraintProvider: null, allowPrereleaseVersions: false,
                preferListedPackages: false, dependencyVersion: DependencyVersion.HighestMinor);

            // Assert
            Assert.Equal("B", package1.Id);
            Assert.Equal(new SemanticVersion("1.1"), package1.Version);
            Assert.Equal("B", package2.Id);
            Assert.Equal(new SemanticVersion("1.1"), package2.Version);
            Assert.Equal("B", package3.Id);
            Assert.Equal(new SemanticVersion("1.1"), package3.Version);
            Assert.Equal("B", package4.Id);
            Assert.Equal(new SemanticVersion("1.0"), package4.Version);
            Assert.Equal("B", package5.Id);
            Assert.Equal(new SemanticVersion("1.1"), package5.Version);
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
            IPackage package1 = repository.ResolveDependency(dependency1, constraintProvider: null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.HighestPatch);
            IPackage package2 = repository.ResolveDependency(dependency2, constraintProvider: null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.HighestPatch);
            IPackage package3 = repository.ResolveDependency(dependency3, constraintProvider: null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.HighestPatch);
            IPackage package4 = repository.ResolveDependency(dependency4, constraintProvider: null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.HighestPatch);
            IPackage package5 = repository.ResolveDependency(dependency5, constraintProvider: null, allowPrereleaseVersions: false, preferListedPackages: false, dependencyVersion: DependencyVersion.HighestPatch);

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

        private static IPackageRepository GetEmptyRepository()
        {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            repository.Setup(c => c.GetPackages()).Returns(() => Enumerable.Empty<IPackage>().AsQueryable());
            return repository.Object;
        }

        private static IPackageRepository GetRemoteRepository(bool includePrerelease = false)
        {
            Mock<IPackageRepository> repository = new Mock<IPackageRepository>();
            var packages = new List<IPackage> {
                                   CreateMockPackage("A", "1.0", "scripts style"), 
                                   CreateMockPackage("B", "1.0", "testing"), 
                                   CreateMockPackage("C", "2.0", "xaml"), 
                                   CreateMockPackage("A", "1.2", "a updated desc") };
            if (includePrerelease)
            {
                packages.Add(CreateMockPackage("A", "2.0-alpha", "a prerelease package"));
                packages.Add(CreateMockPackage("B", "1.0-beta", "another prerelease package"));
            }

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
            package.SetupGet(p => p.Listed).Returns(true);
            return package.Object;
        }
    }
}
