using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public partial class VsPackageManagerTest
    {
        [Fact]
        public void InstallPackageSetOperationToInstall1()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Install", sourceRepository.LastOperation);
            Assert.Equal("foo", sourceRepository.LastMainPackageId);
            Assert.Equal("1.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void InstallPackageSetOperationToInstall2()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            // Act
            packageManager.InstallPackage(projectManager, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, allowPrereleaseVersions: false, skipAssemblyReferences: false, logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Install", sourceRepository.LastOperation);
            Assert.Equal("foo", sourceRepository.LastMainPackageId);
            Assert.Equal("1.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void InstallPackageSetOperationToInstall3()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), new MockPackageRepository());
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" });
            sourceRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("bar", "2.0", new[] { "world" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.InstallPackage(
                projectManager,
                package2,
                new PackageOperation[] { 
                    new PackageOperation(package, PackageAction.Install),
                    new PackageOperation(package2, PackageAction.Install),
                },
                ignoreDependencies: false, 
                allowPrereleaseVersions: false, 
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Install", sourceRepository.LastOperation);
            Assert.Equal("bar", sourceRepository.LastMainPackageId);
            Assert.Equal("2.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackagesSetOperationToUpdate1()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.UpdatePackages(
                projectManager,
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);
                
            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("2.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackagesSetOperationToUpdate2()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.UpdatePackages(
                projectManager,
                new [] { package },
                new PackageOperation[] { new PackageOperation(package, PackageAction.Install)},
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Null(sourceRepository.LastMainPackageId);
            Assert.Null(sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackagesSetOperationToUpdate3()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);
            var packageManager = new VsPackageManager(TestUtils.GetSolutionManager(), sourceRepository, new Mock<IFileSystemProvider>().Object, projectSystem, localRepository, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            var packageB = PackageUtility.CreatePackage("time", "1.0", new[] { "hello" });
            projectRepository.AddPackage(package);

            // Act
            packageManager.UpdatePackage(
                projectManager,
                "phuong",
                version: null,
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("2.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackageSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new [] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new [] { project }),
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                projectSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.UpdatePackage(
                "phuong",
                new SemanticVersion("2.0"),
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance,
                eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("2.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackageWithVersionSpecSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.UpdatePackage(
                "phuong",
                new VersionSpec(new SemanticVersion("1.0")),
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance,
                eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Null(sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void UpdatePackageWithProjectManagerSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.UpdatePackage(
                projectManager,
                "phuong",
                new SemanticVersion("2.0"),
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("2.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void SafeUpdatePackageSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "1.0.0.1", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.SafeUpdatePackages(
                projectManager,
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("1.0.0.1", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void SafeUpdateOnePackageSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "2.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.SafeUpdatePackage(
                "phuong",
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance,
                eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
        }

        [Fact]
        public void SafeUpdatePackageWithPackageIdSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);

            var package2 = PackageUtility.CreatePackage("phuong", "1.0.4.0", new[] { "hello" });
            sourceRepository.AddPackage(package2);

            // Act
            packageManager.SafeUpdatePackage(
                projectManager,
                "phuong",
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Update", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
            Assert.Equal("1.0.4.0", sourceRepository.LastMainPackageVersion);
        }

        [Fact]
        public void ReinstallAllPackagesSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            packageManager.ReinstallPackages(
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance,
                eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            Assert.Equal("Reinstall", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
        }

        [Fact]
        public void ReinstallAllPackagesInOneProjectSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            packageManager.ReinstallPackages(
                projectManager,
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Reinstall", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
        }

        [Fact]
        public void ReinstallOnePackageSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            packageManager.ReinstallPackage(
                "phuong",
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance,
                eventListener: new Mock<IPackageOperationEventListener>().Object);

            // Assert
            Assert.Equal("Reinstall", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
        }

        [Fact]
        public void ReinstallOnePackageInOneProjectSetOperationToUpdate()
        {
            // Arrange
            var localRepository = new MockSharedPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(projectSystem);
            var projectManager = new ProjectManager(localRepository, pathResolver, new MockProjectSystem(), projectRepository);

            var project = TestUtils.GetProject("project runway", projectFiles: new[] { "dotnetjunky.cs" });

            var packageManager = new MockVsPackageManager(
                TestUtils.GetSolutionManager(defaultProjectName: "project runway", projects: new[] { project }),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                projectSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object);
            packageManager.RegisterProjectManager(project, projectManager);

            var package = PackageUtility.CreatePackage("phuong", "1.0", new[] { "hello" });
            localRepository.AddPackage(package);
            projectRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            packageManager.ReinstallPackage(
                projectManager,
                "phuong",
                updateDependencies: true,
                allowPrereleaseVersions: true,
                logger: NullLogger.Instance);

            // Assert
            Assert.Equal("Reinstall", sourceRepository.LastOperation);
            Assert.Equal("phuong", sourceRepository.LastMainPackageId);
        }
    }
}