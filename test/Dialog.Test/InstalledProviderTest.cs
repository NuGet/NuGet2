using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace NuGet.Dialog.Test
{


    public class InstalledProviderTest
    {

        [Fact]
        public void NamePropertyIsCorrect()
        {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act & Assert
            Assert.Equal("Installed packages", provider.Name);
        }

        [Fact]
        public void RefresOnNodeSelectionPropertyIsCorrect()
        {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act & Assert
            Assert.True(provider.RefreshOnNodeSelection);
        }

        [Fact]
        public void VerifySortDescriptors()
        {
            // Arrange
            var provider = CreateInstalledProvider();

            // Act
            var descriptors = provider.SortDescriptors.Cast<PackageSortDescriptor>().ToList();

            // Assert
            Assert.Equal(2, descriptors.Count);
            Assert.Equal("Title", descriptors[0].SortProperties.First());
            Assert.Equal("Id", descriptors[0].SortProperties.Last());
            Assert.Equal(ListSortDirection.Ascending, descriptors[0].Direction);
            Assert.Equal("Title", descriptors[1].SortProperties.First());
            Assert.Equal("Id", descriptors[1].SortProperties.Last());
            Assert.Equal(ListSortDirection.Descending, descriptors[1].Direction);
        }

        [Fact]
        public void RootNodeIsPopulatedWithOneNode()
        {
            // Arrange            
            var provider = CreateInstalledProvider();

            // Act
            var extentionsTree = provider.ExtensionsTree;

            // Assert
            Assert.Equal(1, extentionsTree.Nodes.Count);
            Assert.IsType(typeof(SimpleTreeNode), extentionsTree.Nodes[0]);
        }

        [Fact]
        public void CreateExtensionReturnsAPackageItem()
        {
            // Arrange
            var provider = CreateInstalledProvider();

            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            IVsExtension extension = provider.CreateExtension(package);

            // Asssert
            Assert.IsType(typeof(PackageItem), extension);
            Assert.Equal("A", extension.Name);
            Assert.Equal("_Uninstall", ((PackageItem)extension).CommandName);
        }

        [Fact]
        public void CanExecuteReturnsCorrectResult()
        {

            // Local repository contains Package A and Package B
            // We test the CanExecute() method on Package A and Package C

            // Arrange
            var repository = new MockPackageRepository();

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(packageA);

            var packageB = PackageUtility.CreatePackage("B", "2.0");
            repository.AddPackage(packageB);

            var packageC = PackageUtility.CreatePackage("C", "2.0");

            var provider = CreateInstalledProvider(null, repository);

            var extensionA = new PackageItem(provider, packageA);
            var extensionC = new PackageItem(provider, packageC);

            // Act
            bool canExecuteA = provider.CanExecute(extensionA);
            bool canExecuteC = provider.CanExecute(extensionC);

            // Assert
            Assert.True(canExecuteA);
            Assert.False(canExecuteC);
        }

        [Fact]
        public void InstalledProviderDoesNotCollapseVersions()
        {
            // Arrange
            var repository = CreateInstalledProvider();

            // Act
            var extentionsTree = repository.ExtensionsTree;

            // Assert
            Assert.Equal(1, extentionsTree.Nodes.Count);

            Assert.IsType(typeof(SimpleTreeNode), extentionsTree.Nodes[0]);
            Assert.Equal("All", extentionsTree.Nodes[0].Name);
            Assert.False(((SimpleTreeNode)extentionsTree.Nodes[0]).CollapseVersions);
        }

        [Fact]
        public void ExecuteMethodCallsUninstallPackageMethodOnPackageManager()
        {
            // Local repository contains Package A

            // Arrange
            var repository = new MockPackageRepository();

            var packageA = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(packageA);

            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(repository);
            projectManager.Setup(p => p.IsInstalled(It.Is<IPackage>(item => item == packageA))).Returns(true);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(It.IsAny<Project>())).Returns(projectManager.Object);

            var provider = CreateInstalledProvider(packageManager.Object, repository);

            var extensionA = new PackageItem(provider, packageA);

            var mockWindowServices = new Mock<IUserNotifierServices>();

            var mre = new ManualResetEventSlim(false);
            provider.ExecuteCompletedCallback = () =>
            {
                // Assert
                packageManager.Verify(p => p.UninstallPackage(projectManager.Object, "A", null, false, false, provider), Times.Once());
                mockWindowServices.Verify(p => p.ShowLicenseWindow(It.IsAny<IEnumerable<IPackage>>()), Times.Never());

                mre.Set();
            };

            // Act
            provider.Execute(extensionA);

            mre.Wait();
        }

        [Fact]
        public void ExecuteMethodInvokesUninstallScriptWhenThePackageContainsOne()
        {
            // Arrange
            var repository = new MockPackageRepository();

            var packageA = PackageUtility.CreatePackage("A", "1.0", tools: new string[] { "uninstall.ps1" });
            repository.AddPackage(packageA);

            var projectManager = CreateProjectManager(repository);

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.UninstallPackage(
                projectManager, It.IsAny<string>(), It.IsAny<SemanticVersion>(), false, false, It.IsAny<ILogger>())).Callback(
                () => projectManager.RemovePackageReference("A"));
            packageManager.Setup(p => p.GetProjectManager(It.IsAny<Project>())).Returns(projectManager);


            var project = new Mock<Project>();
            var scriptExecutor = new Mock<IScriptExecutor>();

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.IsAny<string>())).Returns(project.Object);

            var provider = CreateInstalledProvider(packageManager.Object, null, project.Object, scriptExecutor.Object, solutionManager.Object);

            var extensionA = new PackageItem(provider, packageA);

            var mockLicenseWindowOpener = new Mock<IUserNotifierServices>();

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = () =>
            {
                try
                {
                    // Assert
                    scriptExecutor.Verify(p => p.Execute(It.IsAny<string>(), "uninstall.ps1", packageA, project.Object, It.IsAny<ILogger>()));
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            // Act
            provider.Execute(extensionA);

            manualEvent.Wait();
        }

        private static InstalledProvider CreateInstalledProvider(
            IVsPackageManager packageManager = null,
            IPackageRepository localRepository = null,
            Project project = null,
            IScriptExecutor scriptExecutor = null,
            ISolutionManager solutionManager = null)
        {
            if (packageManager == null)
            {
                packageManager = new Mock<IVsPackageManager>().Object;
            }

            var mockProgressWindowOpener = new Mock<IProgressWindowOpener>();

            if (project == null)
            {
                project = new Mock<Project>().Object;
            }

            if (scriptExecutor == null)
            {
                scriptExecutor = new Mock<IScriptExecutor>().Object;
            }

            var services = new ProviderServices(
                null,
                mockProgressWindowOpener.Object,
                scriptExecutor,
                new MockOutputConsoleProvider()
            );

            if (localRepository == null)
            {
                localRepository = new MockPackageRepository();
            }

            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            return new InstalledProvider(
                packageManager,
                project,
                localRepository,
                new System.Windows.ResourceDictionary(),
                services,
                new Mock<IProgressProvider>().Object,
                solutionManager,
                new Mock<IFileOperations>().Object);
        }

        private static ProjectManager CreateProjectManager(IPackageRepository localRepository)
        {
            var projectSystem = new MockVsProjectSystem();
            return new ProjectManager(new MockPackageRepository(), new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
        }
    }
}