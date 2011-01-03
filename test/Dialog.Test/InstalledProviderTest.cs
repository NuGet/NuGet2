using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test {

    [TestClass]
    public class InstalledProviderTest {

        [TestMethod]
        public void NamePropertyIsCorrect() {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act & Assert
            Assert.AreEqual("Installed packages", provider.Name);
        }

        [TestMethod]
        public void RefresOnNodeSelectionPropertyIsCorrect() {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act & Assert
            Assert.IsTrue(provider.RefreshOnNodeSelection);
        }

        [TestMethod]
        public void VerifySortDescriptors() {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act
            var descriptors = provider.SortDescriptors.Cast<PackageSortDescriptor>().ToList();

            // Assert
            Assert.AreEqual(2, descriptors.Count);
            Assert.AreEqual("Id", descriptors[0].Name);
            Assert.AreEqual(ListSortDirection.Ascending, descriptors[0].Direction);
            Assert.AreEqual("Id", descriptors[1].Name);
            Assert.AreEqual(ListSortDirection.Descending, descriptors[1].Direction);
        }

        [TestMethod]
        public void RootNodeIsPopulatedWithOneNode() {
            // Arrange
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(new MockPackageRepository());
            
            var provider = CreateInstalledProvider(null, projectManager.Object);

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.AreEqual(1, extentionsTree.Nodes.Count);
            Assert.IsInstanceOfType(extentionsTree.Nodes[0], typeof(SimpleTreeNode));
        }

        [TestMethod]
        public void CreateExtensionReturnsAPackageItem() {
            // Arrange
            var provider = CreateInstalledProvider();

            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            IVsExtension extension = provider.CreateExtension(package);

            // Asssert
            Assert.IsInstanceOfType(extension, typeof(PackageItem));
            Assert.AreEqual("A", extension.Name);
            Assert.AreEqual("_Uninstall", ((PackageItem)extension).CommandName);
        }

        [TestMethod]
        public void CanExecuteReturnsCorrectResult() {

            // Local repository contains Package A and Package B
            // We test the CanExecute() method on Package A and Package C

            // Arrange
            var repository = new MockPackageRepository();

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(packageA);

            var packageB = PackageUtility.CreatePackage("B", "2.0");
            repository.AddPackage(packageB);

            var packageC = PackageUtility.CreatePackage("C", "2.0");

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.IsInstalled(It.IsAny<IPackage>())).Returns<IPackage>(p => repository.Exists(p));

            var provider = CreateInstalledProvider(null, projectManager.Object);

            var extensionA = new PackageItem(provider, packageA, null);
            var extensionC = new PackageItem(provider, packageC, null);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.IsTrue(canExecuteA);
            Assert.IsFalse(canExecuteC);
        }

        [TestMethod]
        public void ExecuteMethodCallsUninstallPackageMethodOnPackageManager() {
            // Local repository contains Package A and Package B

            // Arrange
            var repository = new MockPackageRepository();

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(packageA);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(repository);

            var packageManager = new Mock<IVsPackageManager>();

            var provider = CreateInstalledProvider(packageManager.Object, projectManager.Object);

            var extensionA = new PackageItem(provider, packageA, null);

            var mockLicenseWindowOpener = new Mock<ILicenseWindowOpener>();

            // Act
            provider.Execute(extensionA);

            // Assert
            packageManager.Verify(p => p.UninstallPackage(projectManager.Object, "A", null, false, false, provider), Times.Once());
            mockLicenseWindowOpener.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());
        }

        private static InstalledProvider CreateInstalledProvider(IVsPackageManager packageManager = null, IProjectManager projectManager = null) {
            if (packageManager == null) {
                packageManager = new Mock<IVsPackageManager>().Object;
            }

            if (projectManager == null) {
                projectManager = new Mock<IProjectManager>().Object;
            }

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();

            return new InstalledProvider(packageManager, null, projectManager, new System.Windows.ResourceDictionary(), mockProgressWindowOpener.Object, null);
        }
    }
}
