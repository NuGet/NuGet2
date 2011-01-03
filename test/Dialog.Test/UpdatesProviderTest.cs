using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
    public class UpdatesProviderTest {

        [TestMethod]
        public void NamePropertyIsCorrect() {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act & Assert
            Assert.AreEqual("Updates", provider.Name);
        }

        [TestMethod]
        public void RefresOnNodeSelectionPropertyIsCorrect() {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act & Assert
            Assert.IsTrue(provider.RefreshOnNodeSelection);
        }

        [TestMethod]
        public void VerifySortDescriptors() {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act
            var descriptors = provider.SortDescriptors.Cast<PackageSortDescriptor>().ToList();

            // Assert
            Assert.AreEqual(4, descriptors.Count);
            Assert.AreEqual("Rating", descriptors[0].Name);
            Assert.AreEqual(ListSortDirection.Descending, descriptors[0].Direction);
            Assert.AreEqual("DownloadCount", descriptors[1].Name);
            Assert.AreEqual(ListSortDirection.Descending, descriptors[1].Direction);
            Assert.AreEqual("Id", descriptors[2].Name);
            Assert.AreEqual(ListSortDirection.Ascending, descriptors[2].Direction);
            Assert.AreEqual("Id", descriptors[3].Name);
            Assert.AreEqual(ListSortDirection.Descending, descriptors[3].Direction);
        }

        [TestMethod]
        public void RootNodeIsPopulatedWithOneNode() {
            // Arrange
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(new MockPackageRepository());
            projectManager.Setup(p => p.SourceRepository).Returns(new MockPackageRepository());

            var provider = CreateUpdatesProvider(null, projectManager.Object);

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.AreEqual(1, extentionsTree.Nodes.Count);
            Assert.IsInstanceOfType(extentionsTree.Nodes[0], typeof(UpdatesTreeNode));
        }

        [TestMethod]
        public void CreateExtensionReturnsAPackageItem() {
            // Arrange
            var provider = CreateUpdatesProvider();

            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            IVsExtension extension = provider.CreateExtension(package);

            // Asssert
            Assert.IsInstanceOfType(extension, typeof(PackageItem));
            Assert.AreEqual("A", extension.Name);
            Assert.AreEqual("_Update", ((PackageItem)extension).CommandName);
        }

        [TestMethod]
        public void CanExecuteReturnsCorrectResult() {
            // Local repository contains Package A 1.0 and Package B
            // Source repository contains Package A 2.0 and Package C

            var packageA1 = PackageUtility.CreatePackage("A", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            // Arrange
            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA1);
            localRepository.AddPackage(packageB);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageC);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            var provider = CreateUpdatesProvider(packageManager.Object, projectManager.Object);

            var extensionA = new PackageItem(provider, packageA2, null);
            var extensionC = new PackageItem(provider, packageC, null);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.IsTrue(canExecuteA);
            Assert.IsFalse(canExecuteC);
        }

        [TestMethod]
        public void ExecuteMethodCallsUpdatePackageMethodOnPackageManager() {
            // Local repository contains Package A 1.0 and Package B
            // Source repository contains Package A 2.0 and Package C

            var packageA1 = PackageUtility.CreatePackage("A", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            // Arrange
            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA1);
            localRepository.AddPackage(packageB);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageC);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            var mockLicenseWindowOpener = new Mock<ILicenseWindowOpener>();
            var provider = CreateUpdatesProvider(packageManager.Object, projectManager.Object, mockLicenseWindowOpener.Object);

            var extensionA = new PackageItem(provider, packageA2, null);
            var extensionC = new PackageItem(provider, packageC, null);

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate {
                // Assert
                mockLicenseWindowOpener.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());
                packageManager.Verify(p => p.UpdatePackage(projectManager.Object, packageA2, It.IsAny<IEnumerable<PackageOperation>>(), true, provider), Times.Once());

                manualEvent.Set();
            };

            // Act
            provider.Execute(extensionA);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        private static UpdatesProvider CreateUpdatesProvider(IVsPackageManager packageManager = null, IProjectManager projectManager = null, ILicenseWindowOpener licenseWindowOpener = null) {
            if (packageManager == null) {
                packageManager = new Mock<IVsPackageManager>().Object;
            }

            if (projectManager == null) {
                projectManager = new Mock<IProjectManager>().Object;
            }

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();
            return new UpdatesProvider(packageManager, null, projectManager, new System.Windows.ResourceDictionary(), licenseWindowOpener, mockProgressWindowOpener.Object, null);
        }
    }
}
