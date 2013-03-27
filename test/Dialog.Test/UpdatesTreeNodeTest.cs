using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Dialog.Test
{
    public class UpdatesTreeNodeTest
    {
        [Fact]
        public void PropertyNameIsCorrect()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            string category = "Mock node";
            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: true, category: category);

            // Act & Assert
            Assert.Equal(category, node.Name);
        }

        [Fact]
        public void GetPackagesReturnsUpdatesForPackages()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesReturnsUpdatesWhenThereAreMultipleVersionsOfTheSamePackageId()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockServiceBasePackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "9.0-rtm"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "9.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "10.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "9.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "4.0-beta"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("C", "1.2.3.4-alpha"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "9.5");
            AssertPackage(packages[1], "B", "4.0-beta");
        }

        [Fact]
        public void GetPackagesReturnsUpdatesConformingToVersionConstraints()
        {
            // Arrange
            var localRepository = new Mock<IPackageRepository>();
            localRepository.Setup(l => l.GetPackages()).Returns(new IPackage[] { PackageUtility.CreatePackage("A", "1.0") }.AsQueryable());

            Mock<IPackageConstraintProvider> constraintProvider = localRepository.As<IPackageConstraintProvider>();
            constraintProvider.Setup(c => c.GetConstraint("A")).Returns(VersionUtility.ParseVersionSpec("(1.0,2.0]"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.5"));

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository.Object, sourceRepository, includePrerelease: false);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesReturnsUpdatesIgnoreConstraintIfConstraintIsNull()
        {
            // Arrange
            var localRepository = new Mock<IPackageRepository>();
            localRepository.Setup(l => l.GetPackages()).Returns(new IPackage[] { PackageUtility.CreatePackage("A", "1.0") }.AsQueryable());

            Mock<IPackageConstraintProvider> constraintProvider = localRepository.As<IPackageConstraintProvider>();
            constraintProvider.Setup(c => c.GetConstraint("A")).Returns((IVersionSpec)null);

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "2.5"));

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository.Object, sourceRepository, includePrerelease: false);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "2.5");
        }

        [Fact]
        public void GetPackagesReturnsNoResultsIfPackageDoesNotExistInSourceRepository()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "1.5"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }

        [Fact]
        public void GetPackagesIngoresLowerVersions()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "0.9"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesReturnsUpdatesForEachPackageFoundInTheSourceRepository()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: false);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            packages.Sort(PackageComparer.Version);

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.9");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesReturnsPrereleasePackagesIfIncludePrereleaseIsTrue()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateUpdatesTreeNode(localRepository, sourceRepository, includePrerelease: true);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9-alpha"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();
            packages.Sort(PackageComparer.Version);      

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.9-alpha");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesOnlyReturnPackagesCompatibleWithTheProjects()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9", assemblyReferences: new string[] { "lib\\sl4\\a.dll" }));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new string[] { "lib\\net20\\b.dll" }));

            PackagesProviderBase provider = new MockPackagesProvider(new string[] { ".NETFramework,Version=3.0" });

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            packages.Sort(PackageComparer.Version);

            // Assert
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
            AssertPackage(packages[1], "B", "2.0");
        }

        [Fact]
        public void GetPackagesCacheResultsWhenIncludePrereleaseIsFalse()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: false).ToList();

            // Assert 1
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");

            // Act 2

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: false).ToList();

            // Assert 2
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [Fact]
        public void GetPackagesCacheResultsWhenIncludePrereleaseIsTrue()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5-alpha"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert 1
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");

            // Act 2

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0-beta"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert 2
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");
        }

        [Fact]
        public void GetPackagesDoesNotCacheResultsWhenIncludePrereleaseValueIsDifferent()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5-alpha"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: false).ToList();

            // Assert 1
            Assert.Equal(0, packages.Count);

            // Act 2

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0-beta"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();
            packages.Sort(PackageComparer.Version);

            // Assert 2
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");
            AssertPackage(packages[1], "B", "2.0-beta");
        }

        [Fact]
        public void GetPackagesDoesNotCacheResultsWhenSearchTermIsNotNull()
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5-alpha"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: "A", allowPrereleaseVersions: true).ToList();

            // Assert 1
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");

            // Act 2

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0-beta"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();
            packages.Sort(PackageComparer.Version);

            // Assert 2
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");
            AssertPackage(packages[1], "B", "2.0-beta");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackagesDoesNotCacheResultsWhenRefreshIsCalled(bool resetQueryBeforeRefresh)
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5-alpha"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert 1
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");

            // Act 2
            node.Refresh(resetQueryBeforeRefresh);

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0-beta"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();
            packages.Sort(PackageComparer.Version);

            // Assert 2
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");
            AssertPackage(packages[1], "B", "2.0-beta");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackagesDoesNotCacheResultsWhenOnClosedIsCalled(bool resetQueryBeforeRefresh)
        {
            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            MockPackageRepository sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5-alpha"));


            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository);

            // Act 1
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert 1
            Assert.Equal(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");

            // Act 2
            node.OnClosed();

            // now we modify the source repository to test if the GetPackages() return the old results
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0-beta"));
            packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();
            packages.Sort(PackageComparer.Version);

            // Assert 2
            Assert.Equal(2, packages.Count);
            AssertPackage(packages[0], "A", "1.5-alpha");
            AssertPackage(packages[1], "B", "2.0-beta");
        }

        [Fact]
        public void GetPackagesFindsUpdatesForFilteredSetOfPackages()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("Foo", "1.0");
            var packageB = PackageUtility.CreatePackage("Qux", "1.0");
            var localRepository = new MockPackageRepository { packageA, packageB };
            IEnumerable<IPackage> actual = null;

            var sourceRepository = new Mock<IServiceBasedRepository>(MockBehavior.Strict);
            sourceRepository.Setup(s => s.GetUpdates(It.IsAny<IEnumerable<IPackage>>(), true, false, It.IsAny<IEnumerable<FrameworkName>>(), It.IsAny<IEnumerable<IVersionSpec>>()))
                            .Returns(new[] { PackageUtility.CreatePackage("Foo", "1.1") })
                            .Callback((IEnumerable<IPackage> a, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> frameworks, IEnumerable<IVersionSpec> constraints) => actual = a)
                            .Verifiable();

            PackagesProviderBase provider = new MockPackagesProvider(new string[] { ".NETFramework,Version=3.0" });

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            var node = new UpdatesTreeNode(provider, "Mock", parentTreeNode, localRepository, sourceRepository.As<IPackageRepository>().Object);

            // Act
            var result = node.GetPackages(searchTerm: "Foo", allowPrereleaseVersions: true).ToList();

            // Assert
            sourceRepository.Verify();
            Assert.Equal(new[] { packageA }, actual);
            AssertPackage(result.Single(), "Foo", "1.1");
        }

        private static void AssertPackage(IPackage package, string id, string version = null)
        {
            Assert.NotNull(package);
            Assert.Equal(id, package.Id);
            if (version != null)
            {
                Assert.Equal(new SemanticVersion(version), package.Version);
            }
        }

        private static UpdatesTreeNode CreateUpdatesTreeNode(IPackageRepository localRepository, IPackageRepository sourceRepository, bool includePrerelease, string category = "Mock node")
        {
            PackagesProviderBase provider = new MockPackagesProvider();
            provider.IncludePrerelease = includePrerelease;
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new UpdatesTreeNode(provider, category, parentTreeNode, localRepository, sourceRepository);
        }
    }
}