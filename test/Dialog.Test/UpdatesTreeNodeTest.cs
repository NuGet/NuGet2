using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class UpdatesTreeNodeTest {
        [TestMethod]
        public void PropertyNameIsCorrect() {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            string category = "Mock node";
            UpdatesTreeNode node = CreateSimpleTreeNode(localRepository, sourceRepository, category);

            // Act & Assert
            Assert.AreEqual(category, node.Name);
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackages1() {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateSimpleTreeNode(localRepository, sourceRepository);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));

            // Act
            var packages = node.GetPackages().ToList();
            
            // Assert
            Assert.AreEqual(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackages2() {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateSimpleTreeNode(localRepository, sourceRepository);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "1.5"));

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackages3() {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateSimpleTreeNode(localRepository, sourceRepository);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "0.9"));

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count);
            AssertPackage(packages[0], "A", "1.5");
        }

        [TestMethod]
        public void GetPackagesReturnsCorrectPackages4() {

            // Arrange
            MockPackageRepository localRepository = new MockPackageRepository();
            MockPackageRepository sourceRepository = new MockPackageRepository();

            UpdatesTreeNode node = CreateSimpleTreeNode(localRepository, sourceRepository);

            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.5"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("A", "1.9"));
            sourceRepository.AddPackage(PackageUtility.CreatePackage("B", "2.0"));

            // Act
            var packages = node.GetPackages().ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
            AssertPackage(packages[0], "A", "1.9");
            AssertPackage(packages[1], "B", "2.0");
        }

        private static void AssertPackage(IPackage package, string id, string version = null) {
            Assert.IsNotNull(package);
            Assert.AreEqual(id, package.Id);
            if (version != null) {
                Assert.AreEqual(new Version(version), package.Version);
            }
        }

        private static UpdatesTreeNode CreateSimpleTreeNode(IPackageRepository localRepository, IPackageRepository sourceRepository, string category = "Mock node") {
            PackagesProviderBase provider = new MockPackagesProvider();
            IVsExtensionsTreeNode parentTreeNode = new Mock<IVsExtensionsTreeNode>().Object;
            return new UpdatesTreeNode(provider, category, parentTreeNode, localRepository, sourceRepository);
        }
    }
}
