using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using EnvDTE;
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
    public class SolutionOnlineProviderTest
    {
        [Fact]
        public void ShowPrereleaseComboBoxIsTrue()
        {
            // Arrange
            var provider = CreateSolutionOnlineProvider();

            // Act & Assert
            Assert.True(provider.ShowPrereleaseComboBox);
        }


        [Fact]
        public void SupportsExecuteAllCommandIsFalse()
        {
            // Arrange
            var provider = CreateSolutionOnlineProvider();

            // Act && Arrange
            Assert.False(provider.SupportsExecuteAllCommand);
        }

        [Fact]
        public void CreateExtensionsDoesNotSetCurrentVersionAttribute()
        {
            // Arrange
            var provider = CreateSolutionOnlineProvider();
            var package = PackageUtility.CreatePackage("A", "2.0");

            // Act
            var packageItem = (PackageItem)provider.CreateExtension(package);

            // Assert
            Assert.NotNull(packageItem);
            Assert.Null(packageItem.OldVersion);
        }

        [Fact]
        public void SupportedTargetFrameworksDoNotReturnNullValue()
        {
            // Arrange
            var project1 = new Mock<Project>();
            project1.Setup(p => p.Kind).Returns("yyy");
            project1.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(".NETFramework, Version=4.0");

            var project2 = new Mock<Project>();
            project2.Setup(p => p.Kind).Returns("zzz");
            project2.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(null);

            var project3 = new Mock<Project>();
            project3.Setup(p => p.Kind).Returns("aaa");
            project3.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns("Silverlight, Version=2.0");

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(s => s.GetProjects()).Returns(new[] { project1.Object, project2.Object, project3.Object });

            var provider = CreateSolutionOnlineProvider(solutionManager: solutionManager.Object);

            // Act
            var frameworks = provider.SupportedFrameworks.ToList();

            // Assert
            Assert.Equal(2, frameworks.Count);
            Assert.Equal(".NETFramework, Version=4.0", frameworks[0]);
            Assert.Equal("Silverlight, Version=2.0", frameworks[1]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExecuteMethodCallsInstallPackageMethodOnPackageManager(bool includePrerelease)
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

            var provider = CreateSolutionOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object);
            provider.IncludePrerelease = includePrerelease;
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
                Assert.Equal(RepositoryOperationNames.Install, sourceRepository.LastOperation);

                mockPackageManager.Verify(p => p.InstallPackage(
                    new Project[] { project1, project2 },
                    packageB,
                    It.IsAny<IEnumerable<PackageOperation>>(),
                    false,
                    includePrerelease,
                    provider,
                    provider), Times.Once());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void InstallPackageInvokeInitScript()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", tools: new [] { "init.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);

            var project1 = MockProjectUtility.CreateMockProject("Project1");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);
            packageManager.Setup(p => p.InstallPackage(
                new[] { project1 },
                packageB,
                It.IsAny<IEnumerable<PackageOperation>>(),
                false,
                false,
                It.IsAny<ILogger>(),
                It.IsAny<IPackageOperationEventListener>())).Raises(
                    p => p.PackageInstalled += (o, e) => { },
                    new PackageOperationEventArgs(packageB, null, "x:\\nuget"));

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1 });

            var scriptExecutor = new Mock<IScriptExecutor>();

            var provider = CreateSolutionOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, scriptExecutor: scriptExecutor.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);

            var manualEvent = new ManualResetEventSlim(false);

            Exception callbackException = null;

            provider.ExecuteCompletedCallback = delegate
            {
                try
                {
                    // Assert
                    scriptExecutor.Verify(p => p.Execute("x:\\nuget", "init.ps1", packageB, null, null, It.IsAny<ILogger>()), Times.Once());
                }
                catch (Exception exception)
                {
                    callbackException = exception;
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();

            if (callbackException != null)
            {
                throw callbackException;
            }
        }

        [Fact]
        public void InstallPackageInvokeInstallScript()
        {
            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0", tools: new[] { "install.ps1" });
            var packageC = PackageUtility.CreatePackage("C", "3.0");

            var sourceRepository = new MockPackageRepository();
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageB);

            var localRepository = new MockPackageRepository();

            var fileSystem = new Mock<IVsProjectSystem>();
            fileSystem.SetupGet(f => f.UniqueName).Returns("Project1");

            var projectManager1 = new Mock<IProjectManager>();
            projectManager1.Setup(p => p.LocalRepository).Returns(localRepository);
            projectManager1.Setup(p => p.AddPackageReference(packageB, false, false))
                           .Raises(p => p.PackageReferenceAdded += (o, a) => { }, new PackageOperationEventArgs(packageB, fileSystem.As<IFileSystem>().Object, "x:\\nuget"));

            var project1 = MockProjectUtility.CreateMockProject("Project1");

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.SourceRepository).Returns(sourceRepository);
            packageManager.Setup(p => p.GetProjectManager(It.Is<Project>(s => s == project1))).Returns(projectManager1.Object);
            packageManager.Setup(p => p.IsProjectLevel(It.IsAny<IPackage>())).Returns(true);
            packageManager.Setup(p => p.InstallPackage(
                new[] { project1 },
                packageB,
                It.IsAny<IEnumerable<PackageOperation>>(),
                false,
                false,
                It.IsAny<ILogger>(),
                It.IsAny<IPackageOperationEventListener>()))
                .Callback(
                    (IEnumerable<Project> projects, 
                     IPackage package, 
                     IEnumerable<PackageOperation> operations, 
                     bool ignoreDependencies, 
                     bool allowPrereleaseVersions,
                     ILogger logger, 
                     IPackageOperationEventListener eventListener) =>
                     {
                         eventListener.OnBeforeAddPackageReference(project1);
                         projectManager1.Object.AddPackageReference(packageB, false, false);
                     });

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.GetProject(It.Is<string>(s => s == "Project1"))).Returns(project1);
            solutionManager.Setup(p => p.GetProjects()).Returns(new Project[] { project1 });

            var scriptExecutor = new Mock<IScriptExecutor>();

            var provider = CreateSolutionOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, scriptExecutor: scriptExecutor.Object);
            var extensionTree = provider.ExtensionsTree;

            var firstTreeNode = (SimpleTreeNode)extensionTree.Nodes[0];
            firstTreeNode.Repository.AddPackage(packageA);
            firstTreeNode.Repository.AddPackage(packageB);
            firstTreeNode.Repository.AddPackage(packageC);

            provider.SelectedNode = firstTreeNode;
            IVsPackageManager activePackageManager = provider.GetActivePackageManager();
            Mock<IVsPackageManager> mockPackageManager = Mock.Get<IVsPackageManager>(activePackageManager);

            var manualEvent = new ManualResetEventSlim(false);

            Exception callbackException = null;

            provider.ExecuteCompletedCallback = delegate
            {
                try
                {
                    // Assert
                    scriptExecutor.Verify(p => p.Execute("x:\\nuget", "install.ps1", packageB, project1, It.IsAny<FrameworkName>(), It.IsAny<ILogger>()), Times.Once());
                }
                catch (Exception exception)
                {
                    callbackException = exception;
                }
                finally
                {
                    manualEvent.Set();
                }
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();

            if (callbackException != null)
            {
                throw callbackException;
            }
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

            var mockWindowService = new Mock<IUserNotifierServices>();
            mockWindowService.Setup(p => p.ShowProjectSelectorWindow(
                It.IsAny<string>(),
                It.IsAny<IPackage>(),
                It.IsAny<Predicate<Project>>(),
                It.IsAny<Predicate<Project>>())).Returns((Func<IEnumerable<Project>>)null);

            var provider = CreateSolutionOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
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
                mockPackageManager.Verify(p => p.InstallPackage(
                    It.IsAny<IEnumerable<Project>>(),
                    packageB,
                    It.IsAny<IEnumerable<PackageOperation>>(),
                    false,
                    false,
                    provider,
                    provider), Times.Never());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        [Fact]
        public void ExecuteMethodDoNotCallInstallPackageIfUserDoesNotSelectAnyProject()
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

            var mockWindowService = new Mock<IUserNotifierServices>();
            mockWindowService.Setup(p => p.ShowProjectSelectorWindow(
                It.IsAny<string>(),
                It.IsAny<IPackage>(),
                It.IsAny<Predicate<Project>>(),
                It.IsAny<Predicate<Project>>())).Returns(new Project[0]);

            var provider = CreateSolutionOnlineProvider(packageManager.Object, localRepository, solutionManager: solutionManager.Object, userNotifierServices: mockWindowService.Object);
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
                mockPackageManager.Verify(p => p.InstallPackage(
                    It.IsAny<IEnumerable<Project>>(),
                    packageB,
                    It.IsAny<IEnumerable<PackageOperation>>(),
                    false,
                    false,
                    provider,
                    provider), Times.Never());

                manualEvent.Set();
            };

            var extensionB = new PackageItem(provider, packageB);

            // Act
            provider.Execute(extensionB);

            // do not allow the method to return
            manualEvent.Wait();
        }

        private static SolutionOnlineProvider CreateSolutionOnlineProvider(
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
                var mockWindowServices = new Mock<IUserNotifierServices>();
                mockWindowServices.Setup(p => p.ShowProjectSelectorWindow(
                    It.IsAny<string>(),
                    It.IsAny<IPackage>(),
                    It.IsAny<Predicate<Project>>(),
                    It.IsAny<Predicate<Project>>()))
                .Returns(solutionManager.GetProjects());
                userNotifierServices = mockWindowServices.Object;
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

            return new SolutionOnlineProvider(
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
