using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Dialog.Test
{
    public class UpdatesProviderTest
    {
        [Fact]
        public void NamePropertyIsCorrect()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act & Assert
            Assert.Equal("Updates", provider.Name);
        }

        [Fact]
        public void ShowPrereleaseComboBoxIsTrue()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act & Assert
            Assert.True(provider.ShowPrereleaseComboBox);
        }

        [Fact]
        public void RefresOnNodeSelectionPropertyIsCorrect()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act & Assert
            Assert.True(provider.RefreshOnNodeSelection);
        }

        [Fact]
        public void SupportsExecuteAllCommandIsTrue()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act && Arrange
            Assert.True(provider.SupportsExecuteAllCommand);
        }

        [Fact]
        public void VerifySortDescriptors()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            // Act
            var descriptors = provider.SortDescriptors.Cast<PackageSortDescriptor>().ToList();

            // Assert
            Assert.Equal(4, descriptors.Count);
            Assert.Equal("DownloadCount", descriptors[0].SortProperties.First());
            Assert.Equal(ListSortDirection.Descending, descriptors[0].Direction);

            Assert.Equal("Published", descriptors[1].SortProperties.First());
            Assert.Equal(ListSortDirection.Descending, descriptors[1].Direction);

            Assert.Equal("Title", descriptors[2].SortProperties.First());
            Assert.Equal("Id", descriptors[2].SortProperties.Last());
            Assert.Equal(ListSortDirection.Ascending, descriptors[2].Direction);

            Assert.Equal("Title", descriptors[3].SortProperties.First());
            Assert.Equal("Id", descriptors[3].SortProperties.Last());
            Assert.Equal(ListSortDirection.Descending, descriptors[3].Direction);
        }

        [Fact]
        public void RootNodeIsPopulatedWithCorrectNumberOfChildNodes()
        {
            // Arrange
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(new MockPackageRepository());
            projectManager.Setup(p => p.SourceRepository).Returns(new MockPackageRepository());

            var provider = CreateUpdatesProvider();

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.Equal(3, extentionsTree.Nodes.Count);
            Assert.IsType(typeof(UpdatesTreeNode), extentionsTree.Nodes[0]);
            Assert.Equal("All", extentionsTree.Nodes[0].Name);
            Assert.IsType(typeof(UpdatesTreeNode), extentionsTree.Nodes[1]);
            Assert.Equal("One", extentionsTree.Nodes[1].Name);
            Assert.IsType(typeof(UpdatesTreeNode), extentionsTree.Nodes[2]);
            Assert.Equal("Two", extentionsTree.Nodes[2].Name);
        }

        [Fact]
        public void CreateExtensionReturnsAPackageItem()
        {
            // Arrange
            var provider = CreateUpdatesProvider();

            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            IVsExtension extension = provider.CreateExtension(package);

            // Asssert
            Assert.IsType(typeof(PackageItem), extension);
            Assert.Equal("A", extension.Name);
            Assert.Equal("_Update", ((PackageItem)extension).CommandName);
        }

        [Fact]
        public void CanExecuteReturnsCorrectResult()
        {
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

            var provider = CreateUpdatesProvider(packageManager.Object, localRepository);

            var extensionA = new PackageItem(provider, packageA2);
            var extensionC = new PackageItem(provider, packageC);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.True(canExecuteA);
            Assert.False(canExecuteC);
        }

        [Theory]
        [InlineData("1.0", "2.0")]
        [InlineData("1.0", "2.0-beta")]
        [InlineData("1.0-alpha", "2.0")]
        [InlineData("1.0-alpha", "1.0-beta")]
        public void CreateExtensionAddsOldVersionProperty(string oldVersion, string newVersion)
        {
            // Local repository contains Package A 1.0 and Package B
            // Source repository contains Package A 2.0 and Package C
            var packageA1 = PackageUtility.CreatePackage("A", oldVersion);
            var packageA2 = PackageUtility.CreatePackage("A", newVersion);
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

            var provider = CreateUpdatesProvider(packageManager.Object, localRepository);

            // Act
            var packageItem = (PackageItem)provider.CreateExtension(packageA2);

            // Assert
            Assert.NotNull(packageItem);
            Assert.Equal(oldVersion, packageItem.OldVersion);
            Assert.Equal(newVersion, packageItem.Version);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExecuteMethodCallsUpdatePackageMethodOnPackageManager(bool includePrerelease)
        {
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

            var project = new Mock<Project>();

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager.Object);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var mockWindowServices = new Mock<IUserNotifierServices>();
            var provider = CreateUpdatesProvider(packageManager.Object, localRepository, project: project.Object, userNotifierServices: mockWindowServices.Object, solutionManager: solutionManager.Object);
            provider.IncludePrerelease = includePrerelease;
            var extensionA = new PackageItem(provider, packageA2);
            var extensionC = new PackageItem(provider, packageC);

            provider.SelectedNode = (UpdatesTreeNode)provider.ExtensionsTree.Nodes[0];

            var manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                Assert.Equal(RepositoryOperationNames.Update, sourceRepository.LastOperation);

                mockWindowServices.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());
                packageManager.Verify(p => p.UpdatePackages(projectManager.Object, new [] { packageA2 }, It.IsAny<IEnumerable<PackageOperation>>(), true, includePrerelease, provider), Times.Once());

                manualEvent.Set();
            };

            // Act
            provider.Execute(extensionA);

            // do not allow the method to return
            manualEvent.WaitOne();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExecuteAllMethodCallsUpdatePackagesMethodOnPackageManager(bool includePrerelease)
        {
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

            var project = new Mock<Project>();

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager.Object);

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var mockWindowServices = new Mock<IUserNotifierServices>();
            var provider = CreateUpdatesProvider(packageManager.Object, localRepository, project: project.Object, userNotifierServices: mockWindowServices.Object, solutionManager: solutionManager.Object);
            provider.IncludePrerelease = includePrerelease;
            var extensionA = new PackageItem(provider, packageA2);
            var extensionC = new PackageItem(provider, packageC);

            provider.SelectedNode = (UpdatesTreeNode)provider.ExtensionsTree.Nodes[0];
            var allExtensions = provider.SelectedNode.Extensions;
            allExtensions.Add(extensionA);
            allExtensions.Add(extensionC);

            var manualEvent = new ManualResetEvent(false);

            Exception exception = null;

            provider.ExecuteCompletedCallback = delegate
            {
                try
                {
                    // Assert
                    Assert.Equal(RepositoryOperationNames.Update, sourceRepository.LastOperation);

                    mockWindowServices.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());
                    packageManager.Verify(p => p.UpdatePackages(
                        projectManager.Object, 
                        It.IsAny<IEnumerable<IPackage>>(),
                        It.IsAny<IEnumerable<PackageOperation>>(), 
                        true, 
                        includePrerelease, 
                        provider), Times.Once());
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            // Act
            provider.Execute(item: null);

            // do not allow the method to return
            manualEvent.WaitOne();

            Assert.Null(exception);
        }

        [Fact]
        public void ExecuteMethodInvokeInstallScriptAndUninstallScript()
        {
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

            var project = new Mock<Project>();

            var projectManager = CreateProjectManager(localRepository, sourceRepository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project.Object))).Returns(projectManager);
            packageManager.Setup(p => p.UpdatePackages(
               projectManager, It.IsAny<IEnumerable<IPackage>>(), It.IsAny<IEnumerable<PackageOperation>>(), true, false, It.IsAny<ILogger>())).Callback(
               () =>
               {
                   projectManager.AddPackageReference("A", new SemanticVersion("2.0"), false, false);
               });

            var scriptExecutor = new Mock<IScriptExecutor>();

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProject(It.IsAny<string>())).Returns(project.Object);

            var provider = CreateUpdatesProvider(packageManager.Object, localRepository, null, null, project.Object, scriptExecutor.Object, null, solutionManager.Object);
            provider.SelectedNode = (UpdatesTreeNode)provider.ExtensionsTree.Nodes[0];

            var extensionA = new PackageItem(provider, packageA2);

            ManualResetEvent manualEvent = new ManualResetEvent(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                try
                {
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), "uninstall.ps1", packageA1, project.Object, It.IsAny<FrameworkName>(), It.IsAny<ILogger>()), Times.Once());
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), "install.ps1", packageA2, project.Object, It.IsAny<FrameworkName>(), It.IsAny<ILogger>()), Times.Once());
                }
                finally
                {
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
            IPackageRepository localRepository = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            Project project = null,
            IScriptExecutor scriptExecutor = null,
            IUserNotifierServices userNotifierServices = null,
            ISolutionManager solutionManager = null,
            IUpdateAllUIService updateAllService = null)
        {
            if (packageManager == null)
            {
                var packageManagerMock = new Mock<IVsPackageManager>();
                var sourceRepository = new MockPackageRepository();
                packageManagerMock.Setup(p => p.SourceRepository).Returns(sourceRepository);

                packageManager = packageManagerMock.Object;
            }

            if (localRepository == null)
            {
                localRepository = new Mock<IPackageRepository>().Object;
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

            if (userNotifierServices == null)
            {
                var mockWindowServices = new Mock<IUserNotifierServices>();
                userNotifierServices = mockWindowServices.Object;
            }

            if (project == null)
            {
                project = new Mock<Project>().Object;
            }

            if (scriptExecutor == null)
            {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            var services = new ProviderServices(
                userNotifierServices,
                mockProgressWindowOpener.Object,
                new Mock<IProviderSettings>().Object,
                updateAllService ?? new Mock<IUpdateAllUIService>().Object,
                scriptExecutor,
                new MockOutputConsoleProvider(),
                new Mock<IVsCommonOperations>().Object
            );

            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            return new UpdatesProvider(
                project,
                localRepository,
                new System.Windows.ResourceDictionary(),
                repositoryFactory,
                packageSourceProvider,
                factory.Object,
                services,
                new Mock<IProgressProvider>().Object,
                solutionManager);
        }

        private static ProjectManager CreateProjectManager(IPackageRepository localRepository, IPackageRepository sourceRepository)
        {
            var projectSystem = new MockVsProjectSystem();
            return new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
        }
    }
}