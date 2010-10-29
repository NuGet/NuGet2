using System;
using System.Collections.Generic;
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
    public class OnlineProviderTest {


        [TestMethod]
        public void NamePropertyIsCorrect() {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act & Assert
            Assert.AreEqual("Online", provider.Name);
        }

        [TestMethod]
        public void RefresOnNodeSelectionPropertyIsTrue() {
            // Arrange
            var provider = CreateOnlineProvider();

            // Act & Assert
            Assert.IsTrue(provider.RefreshOnNodeSelection);
        }

        [TestMethod]
        public void RootNodeIsPopulatedWithCorrectNumberOfNodes() {
            var provider = CreateOnlineProvider();

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.AreEqual(2, extentionsTree.Nodes.Count);
            Assert.IsInstanceOfType(extentionsTree.Nodes[0], typeof(SimpleTreeNode));
            Assert.AreEqual("One", extentionsTree.Nodes[0].Name);

            Assert.IsInstanceOfType(extentionsTree.Nodes[1], typeof(SimpleTreeNode));
            Assert.AreEqual("Two", extentionsTree.Nodes[1].Name);
        }

        [TestMethod]
        public void CanExecuteReturnsCorrectResult() {
            // Arrange

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            var provider = CreateOnlineProvider(packageManager.Object, projectManager.Object);

            var extensionA = new PackageItem(provider, packageA, null);
            var extensionB = new PackageItem(provider, packageB, null);
            var extensionC = new PackageItem(provider, packageC, null);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteB = provider.CanExecute(extensionB);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.IsTrue(canExecuteC);
            Assert.IsTrue(canExecuteB);
            Assert.IsFalse(canExecuteA);
        }

        [TestMethod]
        public void ExecuteMethodCallsInstallPackageMethodOnPackageManager() {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);
            
            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            var provider = CreateOnlineProvider(packageManager.Object, projectManager.Object);

            var extensionB = new PackageItem(provider, packageB, null);

            var mockLicenseWindowOpener = new Mock<ILicenseWindowOpener>();

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.InstallCompletedCallback = delegate {
                // Assert
                mockLicenseWindowOpener.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());
                packageManager.Verify(p => p.InstallPackage(projectManager.Object, "B", new Version("2.0"), false), Times.Once());

                manualEvent.Set();
            };

            // Act
            provider.Execute(extensionB, mockLicenseWindowOpener.Object);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        private static OnlineProvider CreateOnlineProvider(
            IVsPackageManager packageManager = null, 
            IProjectManager projectManager = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null) {

            if (packageManager == null) {
                packageManager = new Mock<IVsPackageManager>().Object;
            }

            if (projectManager == null) {
                projectManager = new Mock<IProjectManager>().Object;
            }

            if (repositoryFactory == null) {
                var repositoryFactoryMock = new Mock<IPackageRepositoryFactory>();
                repositoryFactoryMock.Setup(p => p.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());
                repositoryFactory = repositoryFactoryMock.Object;
            }

            if (packageSourceProvider == null) {
                var packageSourceProviderMock = new Mock<IPackageSourceProvider>();
                packageSourceProviderMock.Setup(p => p.GetPackageSources()).Returns(
                        new PackageSource[2] {
                            new PackageSource("One", "Test1"),
                            new PackageSource("Two", "Test2")
                        }
                    );
                packageSourceProvider = packageSourceProviderMock.Object;
            }

            return new OnlineProvider(
                packageManager, 
                projectManager, 
                new System.Windows.ResourceDictionary(),
                repositoryFactory,
                packageSourceProvider);
        }
    }
}
