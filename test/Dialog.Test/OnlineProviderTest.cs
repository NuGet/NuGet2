using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using EnvDTE;
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
        public void VerifySortDescriptors() {
            // Arrange
            var provider = CreateOnlineProvider();

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
        public void RootNodeIsPopulatedWithCorrectNumberOfNodes() {
            // Arrange
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
            projectManager.Setup(p => p.IsInstalled(It.IsAny<IPackage>())).Returns<IPackage>(p => localRepository.Exists(p));

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
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate {
                // Assert
                mockPackageManager.Verify(p => p.InstallPackage(projectManager.Object, packageB, It.IsAny<IEnumerable<PackageOperation>>(), false, provider), Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB, null);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        [TestMethod]
        public void ExecuteMethodInvokesScript() {
            // source repo has A, B, C
            // solution repo has A
            // project repo has C

            // install B

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", content: new string[] { "hello world" }, tools: new string[] { "install.ps1", "uninstall.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var solutionRepository = new MockPackageRepository();
            solutionRepository.AddPackage(packageA);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            var localRepository = new MockPackageRepository();
            localRepository.Add(packageC);

            var projectManager = CreateProjectManager(localRepository, solutionRepository);

            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.LocalRepository).Returns(solutionRepository);

            packageManager.Setup(p => p.InstallPackage(
               projectManager, It.IsAny<IPackage>(), It.IsAny<IEnumerable<PackageOperation>>(), false, It.IsAny<ILogger>())).Callback(
               () => {
                   solutionRepository.AddPackage(packageB);
                   projectManager.AddPackageReference(packageB.Id, packageB.Version);
               });

            var provider = CreateOnlineProvider(packageManager.Object, projectManager, null, null, project.Object, scriptExecutor.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate {
                try {
                    // Assert
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), PowerShellScripts.Install, packageB, project.Object, It.IsAny<ILogger>()), Times.Once());
                }
                finally {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB, null);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        [TestMethod]
        public void ExecuteMethodInstallPackagesWithInitScript() {
            // source repo has A, B, C
            // solution repo has A
            // project repo has C

            // install B

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", content: new string[] { "hello world" }, tools: new string[] { "init.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var solutionRepository = new MockPackageRepository();
            solutionRepository.AddPackage(packageA);

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            var localRepository = new MockPackageRepository();
            localRepository.Add(packageC);

            var projectManager = CreateProjectManager(localRepository, solutionRepository);

            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.LocalRepository).Returns(solutionRepository);
            packageManager.Setup(p => p.InstallPackage(projectManager, packageB, It.IsAny<IEnumerable<PackageOperation>>(), false, It.IsAny<ILogger>())).
                Raises(p => p.PackageInstalled += null, packageManager, new PackageOperationEventArgs(packageB, ""));

            var provider = CreateOnlineProvider(packageManager.Object, projectManager, null, null, project.Object, scriptExecutor.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate {
                try {
                    // Assert

                    // init.ps1 should be executed
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), PowerShellScripts.Init, packageB, null, It.IsAny<ILogger>()), Times.Once());

                    // InstallPackage() should get called
                    packageManager.Verify(p => p.InstallPackage(
                       projectManager, It.IsAny<IPackage>(), It.IsAny<IEnumerable<PackageOperation>>(), false, It.IsAny<ILogger>()), Times.Once());
                }
                finally {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB, null);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        private static OnlineProvider CreateOnlineProvider(
            IVsPackageManager packageManager = null,
            IProjectManager projectManager = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            Project project = null,
            IScriptExecutor scriptExecutor = null) {

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
            var mockLicenseWindowOpener = new Mock<ILicenseWindowOpener>();

            if (project == null) {
                project = new Mock<Project>().Object;
            }

            if (scriptExecutor == null) {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            var services = new ProviderServices(
                mockLicenseWindowOpener.Object,
                mockProgressWindowOpener.Object,
                scriptExecutor,
                new MockOutputConsoleProvider()
            );

            return new OnlineProvider(
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