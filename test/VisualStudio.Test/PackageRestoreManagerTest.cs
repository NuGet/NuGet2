using System;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class PackageRestoreManagerTest : IDisposable
    {
        private static readonly string _testRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfSolutionIsNotOpen()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(false);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfSolutionDirectoryIsNullOrEmpty()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns((string)null);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object);

            // Act 1
            bool enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.False(enabled);

            // Act 2
            solutionManager.Setup(p => p.SolutionDirectory).Returns(String.Empty);
            enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfNuGetFolderDoesNotExist()
        {
            // Arrange
            string path = CreateTempFolder();

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(path);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(p => p.FileExists(It.IsAny<string>())).Returns(false);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(path)).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfNuGetTargetsDoesNotExist()
        {
            // Arrange
            string path = CreateTempFolder();
            Directory.CreateDirectory(Path.Combine(path, ".nuget"));

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(path);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(path)).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsTrueIfFilesAndFoldersExist()
        {
            // Arrange
            string path = CreateTempFolder();
            Directory.CreateDirectory(Path.Combine(path, ".nuget"));
            CreateEmptyFile(Path.Combine(path, ".nuget", "NuGet.exe"));
            CreateEmptyFile(Path.Combine(path, ".nuget", "NuGet.targets"));

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(path);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(path)).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabledForRestore;

            // Assert
            Assert.True(enabled);
        }

        [Fact]
        public void CallingEnableCurrentSolutionThrowsIfSolutionIsNotOpen()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(false);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object);

            // Act & Assert
            Exception exception = Assert.Throws<InvalidOperationException>(() => packageRestore.EnableCurrentSolutionForRestore(fromActivation: false));
            Assert.Equal("The current environment does not have a solution loaded.", exception.Message);
        }

        [Fact]
        public void CallingEnableCurrentSolutionSetupEverythingCorrectly()
        {
            // Arrange
            string tempSolutionPath = CreateTempFolder();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);

            // setup file system
            var fileSystem = new PhysicalFileSystem(tempSolutionPath);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem);

            var nugetFolderFileSystem = new PhysicalFileSystem(tempSolutionPath + "\\.nuget");
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath + "\\.nuget")).Returns(nugetFolderFileSystem);

            // default app settings
            var defaultAppSettings = new Mock<ISettings>();
            defaultAppSettings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns("false");

            // setup DTE
            var dte = new Mock<DTE>();

            var projectItems = new Mock<ProjectItems>();
            var solutionFolder = new Mock<Project>();
            solutionFolder.Setup(s => s.Name).Returns(".nuget");
            solutionFolder.SetupGet(s => s.ProjectItems).Returns(projectItems.Object);

            var solution = new Mock<Solution>();
            solution.As<Solution2>().Setup(p => p.AddSolutionFolder(".nuget")).Returns(solutionFolder.Object);

            var projects = new MockProjects(new Project[0]);
            solution.As<Solution2>().Setup(s => s.Projects).Returns(projects);
            dte.SetupGet(p => p.Solution).Returns(solution.Object);

            // setup package repository
            var packageRepository = new MockPackageRepository();
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Build",
                version: "1.0",
                tools: new string[] { "NuGet.targets" },
                dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") }));
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.CommandLine",
                version: "1.0",
                tools: new string[] { "NuGet.exe" }));
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(NuGetConstants.DefaultFeedUrl)).Returns(packageRepository);
            var packageSourceProvider = new Mock<IVsPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(new[]
                                                                             {
                                                                                 new PackageSource(NuGetConstants.DefaultFeedUrl)
                                                                             });
            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object, 
                packageSourceProvider: packageSourceProvider.Object, 
                settings: defaultAppSettings.Object);

            // Act 
            packageRestore.EnableCurrentSolutionForRestore(fromActivation: false);

            // Assert

            // verify that the files are copied to the .nuget sub folder under solution
            Assert.True(Directory.Exists(Path.Combine(tempSolutionPath, ".nuget")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.exe")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.targets")));

            // verify that solution folder 'nuget' is added to solution
            solution.As<Solution2>().Verify(p => p.AddSolutionFolder(".nuget"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.exe"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.targets"));

            // verify that the Source Control mode is disabled
            var settings = new Settings(nugetFolderFileSystem);
            Assert.True(settings.IsSourceControlDisabled());

            // verify that package restore consent is not set
            defaultAppSettings.Verify(
                s => s.SetValue("packageRestore", "enabled", It.Is<string>(v => v == "true" || v == "1")), Times.Never());

            // clean up
            Directory.Delete(tempSolutionPath, recursive: true);
        }

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithTrueValue()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0], B[1.2-alpha]
            // The solution's packages folder contains only A[1.0]

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>(MockBehavior.Strict);
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem.Object);

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            
            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                fileSystemProvider: fileSystemProvider.Object,
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
                {
                    packagesMissing = e.PackagesMissing;
                };
            
            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(true, packagesMissing);
        }

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithTrueValueIfPackagesFolderIsMissing()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0]
            // The solution's packages folder doesn't contain any packages

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem.Object);

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                fileSystemProvider: fileSystemProvider.Object,
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
            {
                packagesMissing = e.PackagesMissing;
            };

            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(true, packagesMissing);
        }

        [Fact]
        public void CallingCheckForMissingPackagesRaisesThePackagesMissingStatusChangedEventWithFalseValue()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0], B[1.2-alpha]
            // The solution's packages folder contains only A[1.0], B[1.2-alpha]

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem.Object);

            // setup VsPackageManager
            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", 
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                </packages>");

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));
            localRepository.AddPackage(PackageUtility.CreatePackage("B", "1.2-alpha"));

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                fileSystemProvider: fileSystemProvider.Object,
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            bool? packagesMissing = null;
            packageRestore.PackagesMissingStatusChanged += (o, e) =>
            {
                packagesMissing = e.PackagesMissing;
            };

            // Act
            packageRestore.CheckForMissingPackages();

            // Assert
            Assert.Equal(false, packagesMissing);
        }

        [Fact]
        public void CallingRestoreMissingPackagesMethodInstallMissingPackages()
        {
            // Scenario:
            // Project's packages.config specifies: A[1.0]
            // The solution's packages folder contains only A[1.0], B[1.2-alpha], C[2.0-RC1]
            // Call RestoreMissingPackages() will install B[1.2-alpha] and C[2.0-RC1] into the solution

            // Arrange
            string tempSolutionPath = "x:\\project1";

            var project = new Mock<Project>();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);
            solutionManager.Setup(p => p.GetProjects()).Returns(new[] { project.Object });

            // setup file system
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\NuGet.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem.Object);

            // setup VsPackageManager
            string tempFile = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <packages>
                    <package id=""A"" version=""1.0.0.0"" />
                    <package id=""B"" version=""1.2-alpha"" />
                    <package id=""C"" version=""2.0-RC1"" />
                </packages>";

            var projectFileSystem = new MockFileSystem();
            projectFileSystem.AddFile("packages.config", tempFile);

            var packageReferenceRepository = new PackageReferenceRepository(projectFileSystem, projectName: null, sourceRepository: new Mock<ISharedPackageRepository>().Object);
            var projectManager = new Mock<IProjectManager>();
            projectManager.Setup(p => p.LocalRepository).Returns(packageReferenceRepository);

            var localRepository = new MockPackageRepository();
            localRepository.AddPackage(PackageUtility.CreatePackage("A", "1.0"));

            var packageManager = new Mock<IVsPackageManager>();
            packageManager.Setup(p => p.GetProjectManager(project.Object)).Returns(projectManager.Object);
            packageManager.Setup(p => p.LocalRepository).Returns(localRepository);

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager()).Returns(packageManager.Object);
            packageManagerFactory.Setup(p => p.CreatePackageManagerWithAllPackageSources()).Returns(packageManager.Object);

            var packageRestore = CreateInstance(
                fileSystemProvider: fileSystemProvider.Object,
                solutionManager: solutionManager.Object,
                packageManagerFactory: packageManagerFactory.Object);

            Exception exception = null;
            var manualEvent = new ManualResetEventSlim();

            // Act
            packageRestore.RestoreMissingPackages().ContinueWith(
                task => 
                {
                    if (task.IsFaulted)
                    {
                        exception = task.Exception;
                        manualEvent.Set();
                        return;
                    }

                    try
                    {
                        // Assert
                        packageManager.Verify(p => p.InstallPackage("A", new SemanticVersion("1.0.0.0"), true, true), Times.Never());
                        packageManager.Verify(p => p.InstallPackage("B", new SemanticVersion("1.2-alpha"), true, true), Times.Once());
                        packageManager.Verify(p => p.InstallPackage("C", new SemanticVersion("2.0-RC1"), true, true), Times.Once());
                    }
                    catch (Exception testException)
                    {
                        exception = testException;
                    }
                    finally
                    {
                        manualEvent.Set();
                    }
                });

            manualEvent.Wait();

            Assert.Null(exception);
        }

        [Fact]
        public void CallingEnableCurrentSolutionWillAddPackagesToMachineCache()
        {
            // Arrange
            string tempSolutionPath = CreateTempFolder();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);

            // setup file system
            var fileSystem = new PhysicalFileSystem(tempSolutionPath);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem);

            var nugetFolderFileSystem = new PhysicalFileSystem(tempSolutionPath + "\\.nuget");
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath + "\\.nuget")).Returns(nugetFolderFileSystem);

            // setup DTE
            var dte = new Mock<DTE>();

            var projectItems = new Mock<ProjectItems>();
            var solutionFolder = new Mock<Project>();
            solutionFolder.Setup(s => s.Name).Returns(".nuget");
            solutionFolder.SetupGet(s => s.ProjectItems).Returns(projectItems.Object);

            var solution = new Mock<Solution>();
            solution.As<Solution2>().Setup(p => p.AddSolutionFolder(".nuget")).Returns(solutionFolder.Object);

            var projects = new MockProjects(new Project[0]);
            solution.As<Solution2>().Setup(s => s.Projects).Returns(projects);
            dte.SetupGet(p => p.Solution).Returns(solution.Object);

            // setup package repository
            var packageRepository = new MockPackageRepository();
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Build",
                version: "1.0",
                tools: new string[] { "NuGet.targets" },
                dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") }));
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.CommandLine",
                version: "2.0",
                tools: new string[] { "NuGet.exe" }));
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository("x:\\nugetsource")).Returns(packageRepository);

            var packageSourceProvider = new Mock<IVsPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(new[]
                                                                             {
                                                                                 new PackageSource("x:\\nugetsource")
                                                                             });
 
            var localCache = new MockPackageRepository();

            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object,
                localCache: localCache,
                packageSourceProvider: packageSourceProvider.Object);

            // Act 
            packageRestore.EnableCurrentSolutionForRestore(fromActivation: false);

            // Assert
            var cachePackages = localCache.GetPackages().ToList();
            Assert.Equal(2, cachePackages.Count);
            Assert.Equal("NuGet.Build", cachePackages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), cachePackages[0].Version);
            Assert.Equal("NuGet.CommandLine", cachePackages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), cachePackages[1].Version);
        }

        [Fact]
        public void CallingEnableCurrentSolutionDoNotDownloadPackageIfPresentInLocalCache()
        {
            // Arrange
            string tempSolutionPath = CreateTempFolder();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);

            // setup file system
            var fileSystem = new PhysicalFileSystem(tempSolutionPath);
            var fileSystemProvider = new Mock<IFileSystemProvider>(MockBehavior.Strict);
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem);

            var nugetFolderFileSystem = new PhysicalFileSystem(tempSolutionPath + "\\.nuget");
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath + "\\.nuget")).Returns(nugetFolderFileSystem);

            // setup DTE
            var dte = new Mock<DTE>();

            var projectItems = new Mock<ProjectItems>();
            var solutionFolder = new Mock<Project>();
            solutionFolder.Setup(s => s.Name).Returns(".nuget");
            solutionFolder.SetupGet(s => s.ProjectItems).Returns(projectItems.Object);

            var solution = new Mock<Solution>();
            solution.As<Solution2>().Setup(p => p.AddSolutionFolder(".nuget")).Returns(solutionFolder.Object);

            var projects = new MockProjects(new Project[0]);
            solution.As<Solution2>().Setup(s => s.Projects).Returns(projects);
            dte.SetupGet(p => p.Solution).Returns(solution.Object);

            // setup package repository
            var packageRepository = new MockPackageRepository();
            var packageA = new Mock<IPackage>(MockBehavior.Strict);
            packageA.Setup(p => p.Id).Returns("NuGet.Build");
            packageA.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            packageA.Setup(p => p.IsLatestVersion).Returns(true);
            packageA.Setup(p => p.Listed).Returns(true);

            var packageB = new Mock<IPackage>(MockBehavior.Strict);
            packageB.Setup(p => p.Id).Returns("NuGet.CommandLine");
            packageB.Setup(p => p.Version).Returns(new SemanticVersion("2.0"));
            packageB.Setup(p => p.IsLatestVersion).Returns(true);
            packageB.Setup(p => p.Listed).Returns(true);

            packageRepository.AddPackage(packageA.Object);
            packageRepository.AddPackage(packageB.Object);

            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageRepositoryFactory.Setup(p => p.CreateRepository("x:\\nugetsource")).Returns(packageRepository);

            var packageSourceProvider = new Mock<IVsPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(new[]
                                                                             {
                                                                                 new PackageSource("x:\\nugetsource"),
                                                                                 new PackageSource("y:\\me", "unabled", isEnabled: false), 
                                                                             });

            var localCache = new MockPackageRepository();
            localCache.Add(PackageUtility.CreatePackage(
               "NuGet.Build",
               version: "1.0",
               tools: new string[] { "NuGet.targets" },
               dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") },
               listed: true));
            localCache.Add(PackageUtility.CreatePackage(
                "NuGet.CommandLine",
                version: "2.0",
                tools: new string[] { "NuGet.exe" },
                listed: true));

            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object,
                localCache: localCache,
                packageSourceProvider: packageSourceProvider.Object);

            // Act 
            packageRestore.EnableCurrentSolutionForRestore(fromActivation: false);

            // Assert
            packageA.Verify(p => p.GetFiles(), Times.Never());
            packageB.Verify(p => p.GetFiles(), Times.Never());
        }

        [Fact]
        public void CallingEnableCurrentSolutionDownloadPrereleasePackagesButDoNotUnlistedPackage()
        {
            // Arrange
            string tempSolutionPath = CreateTempFolder();

            // setup SolutionManager
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(tempSolutionPath);

            // setup file system
            var fileSystem = new PhysicalFileSystem(tempSolutionPath);
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath)).Returns(fileSystem);

            var nugetFolderFileSystem = new PhysicalFileSystem(tempSolutionPath + "\\.nuget");
            fileSystemProvider.Setup(p => p.GetFileSystem(tempSolutionPath + "\\.nuget")).Returns(nugetFolderFileSystem);

            // default app settings
            var defaultAppSettings = new Mock<ISettings>();
            defaultAppSettings.Setup(s => s.GetValue("packageRestore", "enabled")).Returns("false");

            // setup DTE
            var dte = new Mock<DTE>();

            var projectItems = new Mock<ProjectItems>();
            var solutionFolder = new Mock<Project>();
            solutionFolder.Setup(s => s.Name).Returns(".nuget");
            solutionFolder.SetupGet(s => s.ProjectItems).Returns(projectItems.Object);

            var solution = new Mock<Solution>();
            solution.As<Solution2>().Setup(p => p.AddSolutionFolder(".nuget")).Returns(solutionFolder.Object);

            var projects = new MockProjects(new Project[0]);
            solution.As<Solution2>().Setup(s => s.Projects).Returns(projects);
            dte.SetupGet(p => p.Solution).Returns(solution.Object);

            // setup package repository
            var packageRepository = new MockPackageRepository();
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Build",
                version: "1.0",
                tools: new string[] { "NuGet.targets" },
                dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") }));

            // this package contains 'invalid.targets' in the tools folder. 
            // it shouldn't be installed because it is unlisted.
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Build",
                version: "2.0",
                tools: new string[] { "invalid.targets" },
                dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") },
                listed: false));

            // this verify that we accepts prerelease packages
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.CommandLine",
                version: "1.0-alpha",
                tools: new string[] { "NuGet.exe" }));
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(NuGetConstants.DefaultFeedUrl)).Returns(packageRepository);
            var packageSourceProvider = new Mock<IVsPackageSourceProvider>();
            packageSourceProvider.Setup(p => p.LoadPackageSources()).Returns(new[]
                                                                             {
                                                                                 new PackageSource(NuGetConstants.DefaultFeedUrl)
                                                                             });
            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object,
                packageSourceProvider: packageSourceProvider.Object,
                settings: defaultAppSettings.Object);

            // Act 
            packageRestore.EnableCurrentSolutionForRestore(fromActivation: false);

            // Assert

            // verify that the files are copied to the .nuget sub folder under solution
            Assert.True(Directory.Exists(Path.Combine(tempSolutionPath, ".nuget")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.exe")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.targets")));

            // verify that solution folder 'nuget' is added to solution
            solution.As<Solution2>().Verify(p => p.AddSolutionFolder(".nuget"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.exe"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.targets"));

            // verify that the Source Control mode is disabled
            var settings = new Settings(nugetFolderFileSystem);
            Assert.True(settings.IsSourceControlDisabled());

            // verify that package restore consent is not set
            defaultAppSettings.Verify(
                s => s.SetValue("packageRestore", "enabled", It.Is<string>(v => v == "true" || v == "1")), Times.Never());
        }

        private PackageRestoreManager CreateInstance(
            DTE dte = null,
            ISolutionManager solutionManager = null,
            IFileSystemProvider fileSystemProvider = null,
            IPackageRepositoryFactory packageRepositoryFactory = null,
            IVsThreadedWaitDialogFactory waitDialogFactory = null,
            IVsPackageManagerFactory packageManagerFactory = null,
            IPackageRepository localCache = null,
            IVsPackageSourceProvider packageSourceProvider = null,
            ISettings settings = null)
        {

            if (dte == null)
            {
                dte = new Mock<DTE>().Object;
            }

            if (solutionManager == null)
            {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            if (fileSystemProvider == null)
            {
                fileSystemProvider = new Mock<IFileSystemProvider>().Object;
            }

            if (packageRepositoryFactory == null)
            {
                packageRepositoryFactory = new Mock<IPackageRepositoryFactory>().Object;
            }

            if (waitDialogFactory == null)
            {
                var mockWaitDialogFactory = new Mock<IVsThreadedWaitDialogFactory>();
                var mockWaitDialog = new Mock<IVsThreadedWaitDialog2>();
                mockWaitDialog.Setup(p => p.StartWaitDialog(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()));
                int canceled;
                mockWaitDialog.Setup(p => p.EndWaitDialog(out canceled)).Returns(0);
                var waitDialog = mockWaitDialog.Object;
                mockWaitDialogFactory.Setup(p => p.CreateInstance(out waitDialog)).Returns(0);

                waitDialogFactory = mockWaitDialogFactory.Object;
            }

            if (packageManagerFactory == null)
            {
                packageManagerFactory = new Mock<IVsPackageManagerFactory>().Object;
            }

            if (localCache == null)
            {
                localCache = new MockPackageRepository();
            }

            if (packageSourceProvider == null)
            {
                packageSourceProvider = new Mock<IVsPackageSourceProvider>().Object;
            }

            if (settings == null)
            {
                settings = Mock.Of<ISettings>();
            }

            return new PackageRestoreManager(
                dte,
                solutionManager,
                fileSystemProvider,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory, 
                new VsPackageInstallerEvents(),
                localCache,
                waitDialogFactory,
                settings);
        }

        private string CreateTempFolder()
        {
            string folderPath = Path.Combine(_testRoot, Path.GetRandomFileName());
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        private void CreateEmptyFile(string path)
        {
            File.WriteAllBytes(path, new byte[0]);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testRoot, recursive: true);
            }
            catch { }
        }
    }
}