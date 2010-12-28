using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;

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
        public void GetPackagesReturnsCorrectPackagesBasedOnExtensions2() {
            // Arrange
            PackagesSearchNode node = CreatePackagesSearchNode("B", 5);

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        private static PackagesSearchNode CreatePackagesSearchNode(string searchTerm, int numberOfPackages = 1) {
            PackagesProviderBase provider = new MockPackagesProvider();

            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            MockTreeNode baseTreeNode = new MockTreeNode(parentTreeNode, provider, numberOfPackages);
            return new PackagesSearchNode(provider, parentTreeNode, baseTreeNode, searchTerm);
        }
    }
}
