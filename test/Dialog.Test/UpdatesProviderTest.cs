using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using EnvDTE;
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
            Assert.AreEqual("DownloadCount", descriptors[0].SortProperties.First());
            Assert.AreEqual(ListSortDirection.Descending, descriptors[0].Direction);
            Assert.AreEqual("Rating", descriptors[1].SortProperties.First());
            Assert.AreEqual(ListSortDirection.Descending, descriptors[1].Direction);
            Assert.AreEqual("Title", descriptors[2].SortProperties.First());
            Assert.AreEqual("Id", descriptors[2].SortProperties.Last());
            Assert.AreEqual(ListSortDirection.Ascending, descriptors[2].Direction);
            Assert.AreEqual("Title", descriptors[3].SortProperties.First());
            Assert.AreEqual("Id", descriptors[3].SortProperties.Last());
            Assert.AreEqual(ListSortDirection.Descending, descriptors[3].Direction);
        }

        [TestMethod]
        public void RootNodeIsPopulatedWithCorrectNumberOfChildNodes() {
            // Arrange
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(new MockPackageRepository());
            projectManager.Setup(p => p.SourceRepository).Returns(new MockPackageRepository());

            var provider = CreateUpdatesProvider(null, projectManager.Object);

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.AreEqual(2, extentionsTree.Nodes.Count);
            Assert.IsInstanceOfType(extentionsTree.Nodes[0], typeof(UpdatesTreeNode));
            Assert.AreEqual("One", extentionsTree.Nodes[0].Name);
            Assert.IsInstanceOfType(extentionsTree.Nodes[1], typeof(UpdatesTreeNode));
            Assert.AreEqual("Two", extentionsTree.Nodes[1].Name);
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
            var provider = CreateUpdatesProvider(packageManager: packageManager.Object, projectManager: projectManager.Object, licenseWindowOpener: mockLicenseWindowOpener.Object);

            var extensionA = new PackageItem(provider, packageA2, null);
            var extensionC = new PackageItem(provider, packageC, null);

            provider.SelectedNode = (UpdatesTreeNode)provider.ExtensionsTree.Nodes[0];

            var manualEvent = new ManualResetEvent(false);

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

        [TestMethod]
        public void ExecuteMethodInvokeInstallScriptAndUninstallScript() {
            // Local repository contains Package A 1.0 and Package B
            // Source repository contains Package A 2.0 and Package C

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", tools: new string[] { "uninstall.ps1" });
            var packageA2 = PackageUtility.CreatePackage("A", "2.0", content: new string[] { "hello world" }, tools: new string[] { "install.ps1" });
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            // Arrange
            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA1);
            localRepository.AddPackage(packageB);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageC);

            var projectManager = CreateProjectManager(localRepository, sourceRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);

            packageManager.Setup(p => p.UpdatePackage(
               projectManager, It.IsAny<IPackage>(), It.IsAny<IEnumerable<PackageOperation>>(), true, It.IsAny<ILogger>())).Callback(
               () => {
                   projectManager.AddPackageReference("A", new Version("2.0"), false);
               });

            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var provider = CreateUpdatesProvider(packageManager.Object, projectManager, null, null, project.Object, scriptExecutor.Object, null);
            provider.SelectedNode = (UpdatesTreeNode)provider.ExtensionsTree.Nodes[0];

            var extensionA = new PackageItem(provider, packageA2, null);

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate {
                // Assert
                try {
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), "uninstall.ps1", packageA1, project.Object, It.IsAny<ILogger>()), Times.Once());
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), "install.ps1", packageA2, project.Object, It.IsAny<ILogger>()), Times.Once());
                }
                finally {
                    manualEvent.Set();
                }
            };

            // Act
            provider.Execute(extensionA);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        private static UpdatesProvider CreateUpdatesProvider(
            IVsPackageManager packageManager = null,
            IProjectManager projectManager = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            Project project = null,
            IScriptExecutor scriptExecutor = null,
            ILicenseWindowOpener licenseWindowOpener = null) {

            if (packageManager == null) {
                var packageManagerMock = new Mock<IVsPackageManager>();
                var sourceRepository = new MockPackageRepository();
                packageManagerMock.Setup(p => p.SourceRepository).Returns(sourceRepository);

                packageManager = packageManagerMock.Object;
            }

            if (projectManager == null) {
                projectManager = new Mock<IProjectManager>().Object;
            }

            if (repositoryFactory == null) {
                var repositoryFactoryMock = new Mock<IPackageRepositoryFactory>();
                repositoryFactoryMock.Setup(p => p.CreateRepository(It.IsAny<PackageSource>())).Returns(new MockPackageRepository());
                repositoryFactory = repositoryFactoryMock.Object;
            }

            if (packageSourceProvider == null) {
                var packageSourceProviderMock = new Mock<IPackageSourceProvider>();
                packageSourceProviderMock.Setup(p => p.GetPackageSources()).Returns(
                        new PackageSource[2] {
                            new PackageSource("Test1", "One"),
                            new PackageSource("Test2", "Two")
                        }
                    );
                packageSourceProvider = packageSourceProviderMock.Object;
            }

            var factory = new Mock<IVsPackageManagerFactory>();
            factory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>())).Returns(packageManager);

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();

            if (licenseWindowOpener == null) {
                var mockLicenseWindowOpener = new Mock<ILicenseWindowOpener>();
                licenseWindowOpener = mockLicenseWindowOpener.Object;
            }

            if (project == null) {
                project = new Mock<Project>().Object;
            }

            if (scriptExecutor == null) {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            var services = new ProviderServices(
                licenseWindowOpener,
                mockProgressWindowOpener.Object,
                scriptExecutor,
                new MockOutputConsoleProvider()
            );

            return new UpdatesProvider(
                project,
                projectManager,
                new System.Windows.ResourceDictionary(),
                repositoryFactory,
                packageSourceProvider,
                factory.Object,
                services,
                new Mock<IProgressProvider>().Object);
        }

        private static ProjectManager CreateProjectManager(IPackageRepository localRepository, IPackageRepository sourceRepository) {
            var projectSystem = new MockProjectSystem();
            return new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
        }
    }
}
