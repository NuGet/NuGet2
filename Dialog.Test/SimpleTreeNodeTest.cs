using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class SimpleTreeNodeTest {

        [TestMethod]
        public void PropertyNameIsCorrect() {

            // Arrange
            MockPackageRepository repository = new MockPackageRepository();

            string category = "Mock node";
            SimpleTreeNode node = CreateSimpleTreeNode(repository, category);

            // Act & Assert
            Assert.AreEqual(category, node.Name);
        }

        [TestMethod]
        public void GetPackagesReturnCorrectPackages() {
            // Arrange
            MockPackageRepository repository = new MockPackageRepository();
            
            int numberOfPackages = 3;
            IPackage[] packages = new IPackage[numberOfPackages];
            for (int i = 0; i < numberOfPackages; i++) {
                packages[i] = PackageUtility.CreatePackage("A" + i, "1.0");
                repository.AddPackage(packages[i]);
            }

            SimpleTreeNode node = CreateSimpleTreeNode(repository);

            // Act
            var producedPackages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(packages.Length, producedPackages.Count);

            for (int i = 0; i < numberOfPackages; i++) {
                Assert.AreSame(packages[i], producedPackages[i]);
            }
        }

        private static SimpleTreeNode CreateSimpleTreeNode(IPackageRepository repository, string category = "Mock node") {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new SimpleTreeNode(provider, category, parentTreeNode, repository);
        }
    }
}
