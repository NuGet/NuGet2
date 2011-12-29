using System;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{    
    using PackageUtility = NuGet.Test.PackageUtility;

    public class PackageRestoreManagerTest
    {
        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfSolutionIsNotOpen()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(false);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

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
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.False(enabled);

            // Act 2
            solutionManager.Setup(p => p.SolutionDirectory).Returns(String.Empty);
            enabled = packageRestore.IsCurrentSolutionEnabled;

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
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

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
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.exe")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(path)).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsTrueIfFilesAndFoldersExist()
        {
            // Arrange
            string path = CreateTempFolder();
            Directory.CreateDirectory(Path.Combine(path, ".nuget"));
            CreateEmptyFile(Path.Combine(path, ".nuget", "nuget.exe"));
            CreateEmptyFile(Path.Combine(path, ".nuget", "nuget.targets"));

            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns(path);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem(path)).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

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
            Exception exception = Assert.Throws<InvalidOperationException>(() => packageRestore.EnableCurrentSolution(quietMode: true));
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

            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object);

            // Act 
            packageRestore.EnableCurrentSolution(quietMode: true);

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

            // clean up
            Directory.Delete(tempSolutionPath, recursive: true);
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
            packageRepositoryFactory.Setup(p => p.CreateRepository(NuGetConstants.DefaultFeedUrl)).Returns(packageRepository);

            var localCache = new MockPackageRepository();

            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object,
                localCache: localCache);

            // Act 
            packageRestore.EnableCurrentSolution(quietMode: true);

            // Assert
            var cachePackages = localCache.GetPackages().ToList();
            Assert.Equal(2, cachePackages.Count);
            Assert.Equal("NuGet.Build", cachePackages[0].Id);
            Assert.Equal(new SemanticVersion("1.0"), cachePackages[0].Version);
            Assert.Equal("NuGet.CommandLine", cachePackages[1].Id);
            Assert.Equal(new SemanticVersion("2.0"), cachePackages[1].Version);
            // clean up
            Directory.Delete(tempSolutionPath, recursive: true);
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
            var packageA = new Mock<IPackage>(MockBehavior.Strict);
            packageA.Setup(p => p.Id).Returns("NuGet.Build");
            packageA.Setup(p => p.Version).Returns(new SemanticVersion("1.0"));
            packageA.Setup(p => p.IsLatestVersion).Returns(true);

            var packageB = new Mock<IPackage>(MockBehavior.Strict);
            packageB.Setup(p => p.Id).Returns("NuGet.CommandLine");
            packageB.Setup(p => p.Version).Returns(new SemanticVersion("2.0"));
            packageB.Setup(p => p.IsLatestVersion).Returns(true);

            packageRepository.AddPackage(packageA.Object);
            packageRepository.AddPackage(packageB.Object);
           
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(NuGetConstants.DefaultFeedUrl)).Returns(packageRepository);

            var localCache = new MockPackageRepository();
            localCache.Add(PackageUtility.CreatePackage(
               "NuGet.Build",
               version: "1.0",
               tools: new string[] { "NuGet.targets" },
               dependencies: new PackageDependency[] { new PackageDependency("NuGet.CommandLine") }));
            localCache.Add(PackageUtility.CreatePackage(
                "NuGet.CommandLine",
                version: "2.0",
                tools: new string[] { "NuGet.exe" }));

            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageRepositoryFactory.Object,
                localCache: localCache);

            // Act 
            packageRestore.EnableCurrentSolution(quietMode: true);

            // Assert
            packageA.Verify(p => p.GetFiles(), Times.Never());
            packageB.Verify(p => p.GetFiles(), Times.Never());

            // clean up
            Directory.Delete(tempSolutionPath, recursive: true);
        }

        private PackageRestoreManager CreateInstance(
            DTE dte = null,
            ISolutionManager solutionManager = null,
            IFileSystemProvider fileSystemProvider = null,
            IPackageRepositoryFactory packageRepositoryFactory = null,
            IVsThreadedWaitDialogFactory waitDialogFactory = null,
            IPackageRepository localCache = null)
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

            if (localCache == null)
            {
                localCache = new MockPackageRepository();
            }

            return new PackageRestoreManager(
                dte,
                solutionManager,
                fileSystemProvider,
                packageRepositoryFactory,
                localCache,
                waitDialogFactory);
        }

        private string CreateTempFolder()
        {
            string folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        private void CreateEmptyFile(string path)
        {
            using (FileStream fs = File.Create(path))
            {
                fs.WriteByte(0);
            }
        }
    }
}