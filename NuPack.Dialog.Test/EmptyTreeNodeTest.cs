using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test.Mocks;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class EmptyTreeNodeTest {

        [TestMethod]
        public void PropertyNameIsCorrect() {

            // Arrange
            MockPackageRepository repository = new MockPackageRepository();

            string category = "Mock node";
            EmptyTreeNode node = CreateEmptyTreeNode(category);

            // Act & Assert
            Assert.AreEqual(category, node.Name);
        }

        [TestMethod]
        public void GetPackagesReturnCorrectPackages() {
            // Arrange
            EmptyTreeNode node = CreateEmptyTreeNode();

            // Act
            var producedPackages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, producedPackages.Count);
        }

        private static EmptyTreeNode CreateEmptyTreeNode(string category = "Mock node") {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new EmptyTreeNode(provider, category, parentTreeNode);
        }
    }
}
