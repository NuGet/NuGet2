using System;
using System.Collections.Generic;
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

    public class SolutionUpdatesProviderTest
    {

        [Fact]
        public void ExecuteMethodCallUpdatePackageOnAllProjects()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");
            var packageB2 = PackageUtility.CreatePackage("B", "4.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageB2);

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
                It.IsAny<Predicate<Project>>())).Returns(new Project[] { project1, project2 });

            var provider = CreateSolutionUpdatesProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                mockPackageManager.Verify(p => p.UpdatePackage(
                    new Project[] { project1, project2 },
                    packageB2,
                    It.IsAny<IEnumerable<PackageOperation>>(),
                    true,
                    false,
                    provider,
                    provider), Times.Once());

                manualEvent.Set();
            };

            var extensionB2 = new PackageItem(provider, packageB2);

            // Act
            provider.Execute(extensionB2);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void ExecuteMethodDoNotCallUpdatePackageIfNoProjectIsChecked()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "3.0");
            var packageB2 = PackageUtility.CreatePackage("B", "4.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageB2);

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
                It.IsAny<Predicate<Project>>())).Returns(new Project[0]);

            var provider = CreateSolutionUpdatesProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);

            var manualEvent = new ManualResetEventSlim(false);

            provider.ExecuteCompletedCallback = delegate
            {
                // Assert
                mockPackageManager.Verify(p => p.UpdatePackage(
                    new Project[] { project1, project2 },
                    packageB2,
                    It.IsAny<IEnumerable<PackageOperation>>(),
                    true,
                    false,
                    provider,
                    provider), Times.Never());

                manualEvent.Set();
            };

            var extensionB2 = new PackageItem(provider, packageB2);

            // Act
            provider.Execute(extensionB2);

            // do not allow the method to return
            manualEvent.Wait();
        }

        private static SolutionUpdatesProvider CreateSolutionUpdatesProvider(
            IVsPackageManager packageManager = null,
            IPackageRepository localRepository = null,
            IPackageRepositoryFactory repositoryFactory = null,
            IPackageSourceProvider packageSourceProvider = null,
            IScriptExecutor scriptExecutor = null,
            ISolutionManager solutionManager = null,
            IUserNotifierServices userNotifierServices = null)
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

            if (userNotifierServices == null)
            {
                var mockWindowServices = new Mock<IUserNotifierServices>();
                userNotifierServices = mockWindowServices.Object;
            }

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
                userNotifierServices = new Mock<IUserNotifierServices>().Object;
            }

            var services = new ProviderServices(
                userNotifierServices,
                mockProgressWindowOpener.Object,
                scriptExecutor,
                new MockOutputConsoleProvider()
            );

            if (localRepository == null)
            {
                localRepository = new Mock<IPackageRepository>().Object;
            }

            return new SolutionUpdatesProvider(
                localRepository,
                new System.Windows.ResourceDictionary(),
                repositoryFactory,
                packageSourceProvider,
                factory.Object,
                services,
                new Mock<IProgressProvider>().Object,
                solutionManager);
        }
    }
}
