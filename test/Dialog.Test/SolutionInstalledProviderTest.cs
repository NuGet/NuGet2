using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnvDTE;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.Dialog.Test
{
    public class SolutionInstalledProviderTest
    {
        [Fact]
        public void ShowPrereleaseComboBoxIsFalse()
        {
            // Arrange
            var provider = CreateSolutionInstalledProvider();

            // Act & Assert
            Assert.False(provider.ShowPrereleaseComboBox);
        }

        [Fact]
        public void SupportsExecuteAllCommandIsFalse()
        {
            // Arrange
            var provider = CreateSolutionInstalledProvider();

            // Act && Arrange
            Assert.False(provider.SupportsExecuteAllCommand);
        }

        [Fact]
        public void CreateExtensionsDoesNotSetCurrentVersionAttribute()
        {
            // Arrange
            var provider = CreateSolutionInstalledProvider();
            var package = PackageUtility.CreatePackage("A", "2.0");

            // Act
            var packageItem = (PackageItem)provider.CreateExtension(package);

            // Assert
            Assert.NotNull(packageItem);
            Assert.Null(packageItem.OldVersion);
        }

        [Fact]
        public void ExecuteMethodCallsInstallPackageMethodOnPackageManager()
        {
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

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);

            var projectManager2 = new Mock<IProjectManager>();
            projectManager2.Setup(p => p.LocalRepository).Returns(localRepository);

            var project1 = MockProjectUtility.CreateMockProject("Project1");
            var project2 = MockProjectUtility.CreateMockProject("Project2");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project2))).Returns(projectManager2.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project2"))).Returns(project2);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1, project2 });

            var provider = CreateSolutionInstalledProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            provider.SelectedNode = firstTreeNode;

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                packageManager.Verify(p => p.InstallPackage(
                    projectManager1.Object,
                    packageB.Id,
                    packageB.Version,
                    false,
                    true,
                    provider),
                    Times.Once());

                packageManager.Verify(p => p.InstallPackage(
                    projectManager2.Object,
                    packageB.Id,
                    packageB.Version,
                    false,
                    true,
                    provider),
                    Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void SolutionInstalledProviderShowsAllVersions()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageA2);
            localRepository.AddPackage(packageB);

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);

            var projectManager2 = new Mock<IProjectManager>();
            projectManager2.Setup(p => p.LocalRepository).Returns(localRepository);

            var project1 = MockProjectUtility.CreateMockProject("Project1");
            var project2 = MockProjectUtility.CreateMockProject("Project2");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project2))).Returns(projectManager2.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);
            
            var provider = CreateSolutionInstalledProvider(packageManager.Object, localRepository);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            provider.SelectedNode = firstTreeNode;
            firstTreeNode.IsSelected = true;

            var mre = new ManualResetEventSlim(false);

            firstTreeNode.PackageLoadCompleted += delegate
            {
                var allExtensions = firstTreeNode.Extensions;

                // Assert
                Assert.Equal(3, allExtensions.Count);
                Assert.Equal("A", allExtensions[0].Id);
                Assert.Equal("1.0", ((PackageItem)allExtensions[0]).Version);
                Assert.Equal("A", allExtensions[1].Id);
                Assert.Equal("2.0", ((PackageItem)allExtensions[1]).Version);
                Assert.Equal("B", allExtensions[2].Id);
                Assert.Equal("2.0", ((PackageItem)allExtensions[2]).Version);

                mre.Set();
            };

            // Act
            var ignore = firstTreeNode.Extensions;

            mre.Wait();
        }

        [Fact]
        public void ExecuteMethodCallsUninstallPackageMethodOnPackageManager()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageB);

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);
            projectManager1.Setup(p => p.IsInstalled(It.IsAny<IPackage>())).Returns<IPackage>(p => localRepository.Exists(p));

            var projectManager2 = new Mock<IProjectManager>();
            projectManager2.Setup(p => p.LocalRepository).Returns(localRepository);
            projectManager2.Setup(p => p.IsInstalled(It.IsAny<IPackage>())).Returns<IPackage>(p => localRepository.Exists(p));

            var project1 = MockProjectUtility.CreateMockProject("Project1");
            var project2 = MockProjectUtility.CreateMockProject("Project2");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project2))).Returns(projectManager2.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project2"))).Returns(project2);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1, project2 });

            var mockWindowService = new Mock<IUserNotifierServices>();
            mockWindowService.Setup(p => p.ShowProjectSelectorWindow(
                It.IsAny<string>(),
                It.IsAny<IPackage>(),
                It.IsAny<Predicate<Project>>(),
                It.IsAny<Predicate<Project>>())).Returns(Enumerable.Empty<Project>());

            var provider = CreateSolutionInstalledProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            provider.SelectedNode = firstTreeNode;

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                packageManager.Verify(p => p.UninstallPackage(
                    projectManager1.Object,
                    packageB.Id,
                    null,
                    false,
                    false,
                    provider),
                    Times.Once());

                packageManager.Verify(p => p.UninstallPackage(
                    projectManager2.Object,
                    packageB.Id,
                    null,
                    false,
                    false,
                    provider),
                    Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void ExecuteMethodCallsUninstallPackageMethodForSolutionLevelPackage()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageB);

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);

            var projectManager2 = new Mock<IProjectManager>();
            projectManager2.Setup(p => p.LocalRepository).Returns(localRepository);

            var project1 = MockProjectUtility.CreateMockProject("Project1");
            var project2 = MockProjectUtility.CreateMockProject("Project2");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project2))).Returns(projectManager2.Object);
            // make this a solution-level package
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(false);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project2"))).Returns(project2);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1, project2 });

            var provider = CreateSolutionInstalledProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            provider.SelectedNode = firstTreeNode;

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                packageManager.Verify(p => p.UninstallPackage(
                    null,
                    packageB.Id,
                    packageB.Version,
                    false,
                    false,
                    provider),
                    Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void ExecuteMethodDoNotCallInstallPackageIfUserPressCancelOnTheProjectSelectorButton()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(packageB);

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);

            var localRepository2 = new MockPackageRepository();
            // project2 doesn't have the package installed
            var projectManager2 = new Mock<IProjectManager>();
            projectManager2.Setup(p => p.LocalRepository).Returns(localRepository2);

            var project1 = MockProjectUtility.CreateMockProject("Project1");
            var project2 = MockProjectUtility.CreateMockProject("Project2");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project2))).Returns(projectManager2.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project2"))).Returns(project2);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1, project2 });

            var mockWindowService = new Mock<IUserNotifierServices>();
            mockWindowService.Setup(p => p.ShowProjectSelectorWindow(
                It.IsAny<string>(),
                It.IsAny<IPackage>(),
                It.IsAny<Predicate<Project>>(),
                It.IsAny<Predicate<Project>>())).Returns((Func<IEnumerable<Project>>)null);

            var provider = CreateSolutionInstalledProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            provider.SelectedNode = firstTreeNode;

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                packageManager.Verify(p => p.InstallPackage(
                    projectManager2.Object,
                    packageB.Id,
                    packageB.Version,
                    false,
                    false,
                    provider),
                    Times.Never());

                // Assert
                packageManager.Verify(p => p.UninstallPackage(
                    projectManager1.Object,
                    packageB.Id,
                    null,
                    false,
                    false,
                    provider),
                    Times.Never());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void SolutionInstalledProviderRefreshWhenPackagesAreRestored()
        {
            // Arrange
            var restoreManager = new Mock<IPackageRestoreManager>();

            var provider = CreateSolutionInstalledProvider(packageRestoreManager: restoreManager.Object);

            var treeNode = new Mock<PackagesTreeNodeBase>(null, provider, true);
            provider.SelectedNode = treeNode.Object;

            // Act
            restoreManager.Raise(m => m.PackagesMissingStatusChanged += null, new PackagesMissingStatusEventArgs(false));

            // Assert that the event is unsubscribed
            treeNode.Verify(t => t.Refresh(true), Times.Once());
        }

        [Fact]
        public void SolutionInstalledProviderUnsubscribeToEventWhenDisposed()
        {
            // Arrange
            var restoreManager = new Mock<IPackageRestoreManager>();            

            var provider = CreateSolutionInstalledProvider(packageRestoreManager: restoreManager.Object);

            var treeNode = new Mock<PackagesTreeNodeBase>(null, provider, true);
            provider.SelectedNode = treeNode.Object;

            provider.Dispose();

            // Act
            restoreManager.Raise(m => m.PackagesMissingStatusChanged += null, new PackagesMissingStatusEventArgs(false));

            // Assert that the event is unsubscribed
            treeNode.Verify(t => t.Refresh(It.IsAny<bool>()), Times.Never());
        }

        private static SolutionInstalledProvider CreateSolutionInstalledProvider(
            IVsPackageManager packageManager = null,
            IPackageRepository localRepository = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            IScriptExecutor scriptExecutor = null,
            ISolutionManager solutionManager = null,
            IUserNotifierServices userNotifierServices = null,
            IPackageRestoreManager packageRestoreManager = null)
        {

            if (packageManager == null)
            {
                var packageManagerMock = new Mock<IVsPackageManager>();
                var sourceRepository = new MockPackageRepository();
                packageManagerMock.Setup(p => p.SourceRepository).Returns(sourceRepository);
                
                packageManager = packageManagerMock.Object;
            }

            if (repositoryFactory == null)
            {
                var repositoryFactoryMock = new Mock<IPackageRepositoryFactory>();
                repositoryFactoryMock.Setup(p => p.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());
                repositoryFactory = repositoryFactoryMock.Object;
            }

            if (packageSourceProvider == null)
            {
                var packageSourceProviderMock = new Mock<IPackageSourceProvider>();
                packageSourceProviderMock.Setup(p => p.LoadPackageSources()).Returns(
                        new PackageSource[2] {
                            new PackageSource("Test1", "One"),
                            new PackageSource("Test2", "Two")
                        }
                    );
                packageSourceProvider = packageSourceProviderMock.Object;
            }

            var factory = new Mock<IVsPackageManagerFactory>();
            factory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Returns(packageManager);

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();

            if (scriptExecutor == null)
            {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            if (userNotifierServices == null)
            {
                var mockProjectSelector = new Mock<IUserNotifierServices>();
                mockProjectSelector.Setup(p => p.ShowProjectSelectorWindow(
                    It.IsAny<string>(),
                    It.IsAny<IPackage>(),
                    It.IsAny<Predicate<Project>>(),
                    It.IsAny<Predicate<Project>>()))
                .Returns(solutionManager.GetProjects());
                userNotifierServices = mockProjectSelector.Object;
            }

            var services = new ProviderServices(
                userNotifierServices,
                mockProgressWindowOpener.Object,
                new Mock<IProviderSettings>().Object,
                new Mock<IUpdateAllUIService>().Object,
                scriptExecutor,
                new MockOutputConsoleProvider(),
                new Mock<IVsCommonOperations>().Object
            );

            if (localRepository == null)
            {
                localRepository = new Mock<IPackageRepository>().Object;
            }

            if (packageRestoreManager == null)
            {
                packageRestoreManager = new Mock<IPackageRestoreManager>().Object;
            }

            return new SolutionInstalledProvider(
                packageManager,
                localRepository,
                new System.Windows.ResourceDictionary(),
                services,
                new Mock<IProgressProvider>().Object,
                solutionManager,
                packageRestoreManager);
        }
    }
}
