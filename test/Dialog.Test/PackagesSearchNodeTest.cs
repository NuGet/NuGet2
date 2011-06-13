using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class PackagesSearchNodeTest {

        [TestMethod]
        public void NamePropertyIsValid() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("hello", 1);

            // Act && Assert
            Assert.AreEqual("Search Results", node.Name);
        }

        [TestMethod]
        public void IsSearchResultsNodePropertyIsValid() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("hello", 1);

            // Act && Assert
            Assert.IsTrue(node.IsSearchResultsNode);
        }

        [TestMethod]
        public void SetSearchTextMethodChangesQuery() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("A", 5);

            // Act
            node.SetSearchText("B");
            var packages1 = node.GetPackages().ToList();

            node.SetSearchText("A1");
            var packages2 = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages1.Count);

            Assert.AreEqual(1, packages2.Count);
            Assert.AreEqual("A1", packages2[0].Id);
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackagesBasedOnExtensions() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("A", 5);

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(5, packages.Count);
        }

        [TestMethod]
        public void GetPackagesDoNotCollapseVersionsIfBaseNodeDoesNotDoSo() {
            // Arrange
            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            MockTreeNode baseTreeNode = new MockTreeNode(
                parentTreeNode, 
                provider, 
                new [] {
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
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("Azo", packages[0].Id);
            Assert.AreEqual(new Version("1.0"), packages[0].Version);

            Assert.AreEqual("Azo", packages[1].Id);
            Assert.AreEqual(new Version("2.0"), packages[1].Version);
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackagesBasedOnExtensions2() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("B", 5);

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        private static PackagesSearchNode CreatePackagesSearchNode(string searchTerm, int numberOfPackages = 1, bool collapseVersions = true) {
            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            MockTreeNode baseTreeNode = new MockTreeNode(parentTreeNode, provider, numberOfPackages, collapseVersions);
            return new PackagesSearchNode(provider, parentTreeNode, baseTreeNode, searchTerm);
        }
    }
}
