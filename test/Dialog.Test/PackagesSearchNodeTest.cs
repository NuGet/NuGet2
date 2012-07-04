using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Dialog.Test
{
    public class PackagesSearchNodeTest
    {
        [Fact]
        public void NamePropertyIsValid()
        {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("hello", 1);

            // Act && Assert
            Assert.Equal("Search Results", node.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SupportsPrereleasePackagesMatchBehaviorOfBaseNode(bool supportsPrereleasePackages)
        {
            // Arrange
            var baseNode = new MockTreeNode(
                new Mock<IVsExtensionsTreeNode>().Object, 
                new MockPackagesProvider(), 
                10, 
                true, 
                supportsPrereleasePackages);
            
            PackagesSearchNode node = CreatePackagesSearchNode("yyy", baseNode: baseNode);

            // Act & Assert
            Assert.Equal(supportsPrereleasePackages, node.SupportsPrereleasePackages);
        }

        [Fact]
        public void IsSearchResultsNodePropertyIsValid()
        {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("hello", 1);

            // Act && Assert
            Assert.True(node.IsSearchResultsNode);
        }

        [Fact]
        public void SetSearchTextMethodChangesQuery()
        {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("A", 5);

            // Act
            node.SetSearchText("B");
            var packages1 = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            node.SetSearchText("A1");
            var packages2 = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(0, packages1.Count);

            Assert.Equal(1, packages2.Count);
            Assert.Equal("A1", packages2[0].Id);
        }

        [Fact]
        public void GetPackagesReturnsCorrectPackagesBasedOnExtensions()
        {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("A", 5);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(5, packages.Count);
        }

        [Fact]
        public void GetPackagesReturnsUsesSearchTermPassedInConstructorForSearching()
        {
            // Arrange
            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            MockTreeNode baseTreeNode = new MockTreeNode(
                parentTreeNode,
                provider,
                new[] {
                    PackageUtility.CreatePackage("TestPackage", "1.0"),
                    PackageUtility.CreatePackage("TestPackage", "2.0"),
                    PackageUtility.CreatePackage("Awesome", "1.0"),
                    PackageUtility.CreatePackage("Awesome", "1.2"),
                },
                collapseVersions: false
            );

            var node = new PackagesSearchNode(provider, parentTreeNode, baseTreeNode, "TestPackage");

            // Act
            var packages = node.GetPackages(searchTerm: "Foobar", allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("TestPackage", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);

            Assert.Equal("TestPackage", packages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[1].Version);
        }

        [Fact]
        public void GetPackagesDoNotCollapseVersionsIfBaseNodeDoesNotDoSo()
        {
            // Arrange
            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            MockTreeNode baseTreeNode = new MockTreeNode(
                parentTreeNode,
                provider,
                new[] {
                    PackageUtility.CreatePackage("Azo", "1.0"),
                    PackageUtility.CreatePackage("Azo", "2.0"),
                    PackageUtility.CreatePackage("B", "3.0"),
                    PackageUtility.CreatePackage("B", "4.0"),
                    PackageUtility.CreatePackage("C", "5.0"),
                },
                collapseVersions: false
            );

            var node = new PackagesSearchNode(provider, parentTreeNode, baseTreeNode, "Azo");

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("Azo", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), packages[0].Version);

            Assert.Equal("Azo", packages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[1].Version);
        }

        [Fact]
        public void GetPackagesReturnsCorrectPackagesBasedOnExtensions2()
        {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("B", 5);

            // Act
            var packages = node.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }

        [Fact]
        public void GetPackagesReturnsUpdatesPackageIfBaseNodeIsUpdates()
        {
            // Arrange
            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A1", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B1", "1.0"));

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A1", "3.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B2", "4.0"));

            var updatesPackageNode = CreateUpdatesTreeNode(localRepository, sourceRepository);

            var searchNode = CreatePackagesSearchNode("B", baseNode: updatesPackageNode);

            // Act
            var packages = searchNode.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("B1", packages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Version);
        }

        [Fact]
        public void GetPackagesReturnPrereleasePackagesIfToldSo()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("Azo1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("Azo2", "3.0-alpha"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B2", "4.0"));

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            PackagesProviderBase provider = new MockPackagesProvider()
                                            {
                                                IncludePrerelease = true
                                            };
            var baseNode = new SimpleTreeNode(provider, "Online", parentTreeNode, sourceRepository);

            var searchNode = new PackagesSearchNode(provider, parentTreeNode, baseNode, "Azo");

            // Act
            var packages = searchNode.GetPackages(searchTerm: null, allowPrereleaseVersions: true).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("Azo1", packages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Version);

            Assert.Equal("Azo2", packages[1].Id);
            Assert.Equal(new SemanticVersion("3.0-alpha"), packages[1].Version);
        }

        [Fact]
        public void GetPackagesDoNotReturnPrereleasePackagesIfToldSo()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(PackageUtility.CreatePackage("Azo1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("Azo2", "3.0-alpha"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B1", "2.0"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B2", "4.0"));

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            PackagesProviderBase provider = new MockPackagesProvider();
            var baseNode = new SimpleTreeNode(provider, "Online", parentTreeNode, sourceRepository);

            var searchNode = new PackagesSearchNode(provider, parentTreeNode, baseNode, "Azo");

            // Act
            var packages = searchNode.GetPackages(searchTerm: null, allowPrereleaseVersions: false).ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Equal("Azo1", packages[0].Id);
            Assert.Equal(new SemanticVersion("2.0"), packages[0].Version);
        }

        private static PackagesSearchNode CreatePackagesSearchNode(string searchTerm, int numberOfPackages = 1, bool collapseVersions = true, PackagesTreeNodeBase baseNode = null)
        {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            PackagesTreeNodeBase baseTreeNode = baseNode ?? new MockTreeNode(parentTreeNode, provider, numberOfPackages, collapseVersions);
            return new PackagesSearchNode(provider, parentTreeNode, baseTreeNode, searchTerm);
        }

        private static UpdatesTreeNode CreateUpdatesTreeNode(IPackageRepository localRepository, IPackageRepository sourceRepository, string category = "Mock node")
        {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new UpdatesTreeNode(provider, category, parentTreeNode, localRepository, sourceRepository);
        }
    }
}
