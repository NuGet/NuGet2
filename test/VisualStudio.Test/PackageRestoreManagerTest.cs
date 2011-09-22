using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test {
    using System.IO;
    using PackageUtility = NuGet.Test.PackageUtility;    

    public class PackageRestoreManagerTest {

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfSolutionIsNotOpen() {
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
        public void IsCurrentSolutionEnabledReturnsFalseIfSolutionDirectoryIsNullOrEmpty() {
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
        public void IsCurrentSolutionEnabledReturnsFalseIfNuGetFolderDoesNotExist() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns("c:\\solution");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(p => p.FileExists(It.IsAny<string>())).Returns(false);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem("c:\\solution")).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfNuGetExeDoesNotExist() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns("c:\\solution");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem("c:\\solution")).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsFalseIfNuGetTargetsDoesNotExist() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns("c:\\solution");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.exe")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem("c:\\solution")).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.False(enabled);
        }

        [Fact]
        public void IsCurrentSolutionEnabledReturnsTrueIfFilesAndFoldersExist() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(true);
            solutionManager.Setup(p => p.SolutionDirectory).Returns("c:\\solution");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.DirectoryExists(".nuget")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.exe")).Returns(true);
            fileSystem.Setup(p => p.FileExists(".nuget\\nuget.targets")).Returns(true);

            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(p => p.GetFileSystem("c:\\solution")).Returns(fileSystem.Object);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object, fileSystemProvider: fileSystemProvider.Object);

            // Act
            bool enabled = packageRestore.IsCurrentSolutionEnabled;

            // Assert
            Assert.True(enabled);
        }

        [Fact]
        public void CallingEnableCurrentSolutionThrowsIfSolutionIsNotOpen() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(p => p.IsSolutionOpen).Returns(false);

            var packageRestore = CreateInstance(solutionManager: solutionManager.Object);

            // Act & Assert
            Exception exception = Assert.Throws<InvalidOperationException>(() => packageRestore.EnableCurrentSolution(quietMode: true));
            Assert.Equal("The current environment does not have a solution loaded.", exception.Message);
        }

        [Fact]
        public void CallingEnableCurrentSolutionSetupEverythingCorrectly() {
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

            // setup DTE
            var dte = new Mock<DTE>();

            var projectItems = new Mock<ProjectItems>();
            var solutionFolder = new Mock<Project>();
            solutionFolder.Setup(s => s.Name).Returns("nuget");
            solutionFolder.SetupGet(s => s.ProjectItems).Returns(projectItems.Object);

            var solution = new Mock<Solution>();
            solution.As<Solution2>().Setup(p => p.AddSolutionFolder("nuget")).Returns(solutionFolder.Object);

            var projects = new MockProjects(new Project[0]);
            solution.As<Solution2>().Setup(s => s.Projects).Returns(projects);
            dte.SetupGet(p => p.Solution).Returns(solution.Object);

            // setup package repository
            var packageRepository = new MockPackageRepository();
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Build",
                version: "1.0",
                tools: new string[] { "NuGet.target" },
                dependencies: new PackageDependency[] { new PackageDependency("NuGet.Exe") }));
            packageRepository.Add(PackageUtility.CreatePackage(
                "NuGet.Exe",
                version: "1.0",
                tools: new string[] { "NuGet.exe" }));
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            packageRepositoryFactory.Setup(p => p.CreateRepository(NuGetConstants.DefaultFeedUrl)).Returns(packageRepository);

            // setup package manager
            var packageManager = new Mock<IVsPackageManager>();
            packageManager.SetupGet(p => p.FileSystem).Returns(fileSystem);
            packageManager.Setup(p => p.InstallPackage("NuGet.Build", null, false)).Callback(
                () => {
                    fileSystem.AddFile("packages\\NuGet.Exe\\1.0\\tools\\NuGet.exe", "nuget.exe contents".AsStream());
                    fileSystem.AddFile("packages\\NuGet.Build\\1.0\\tools\\NuGet.target", "nuget.target contents".AsStream());

                    packageManager.Raise(p => p.PackageInstalled += null, new PackageOperationEventArgs(
                        null, fileSystem, Path.Combine(tempSolutionPath, "packages\\NuGet.Exe\\1.0")));

                    packageManager.Raise(p => p.PackageInstalled += null, new PackageOperationEventArgs(
                        null, fileSystem, Path.Combine(tempSolutionPath, "packages\\NuGet.Build\\1.0")));
                });
            packageManager.Setup(p => p.LocalRepository).Returns(new MockPackageRepository());
            packageManager.Setup(p => p.UninstallPackage(
                It.IsAny<string>(),
                It.IsAny<Version>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()));

            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.CreatePackageManager(packageRepository, false, false)).Returns(packageManager.Object);
            
            var packageRestore = CreateInstance(
                dte.Object,
                solutionManager.Object,
                fileSystemProvider.Object,
                packageManagerFactory.Object,
                packageRepositoryFactory.Object);

            // Act 
            packageRestore.EnableCurrentSolution(quietMode: true);

            // Assert

            // verify that the files are copied to the .nuget sub folder under solution
            Assert.True(Directory.Exists(Path.Combine(tempSolutionPath, ".nuget")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.exe")));
            Assert.True(File.Exists(Path.Combine(tempSolutionPath, ".nuget\\NuGet.target")));

            // verify that solution folder 'nuget' is added to solution
            solution.As<Solution2>().Verify(p => p.AddSolutionFolder("nuget"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.exe"));
            projectItems.Verify(p => p.AddFromFile(tempSolutionPath + "\\.nuget\\NuGet.target"));

            // verify that the Source Control mode is disabled
            var settings = new Settings(fileSystem);
            Assert.True(settings.IsSourceControlDisabled());

            // clean up
            Directory.Delete(tempSolutionPath, recursive: true);
        }

        private PackageRestoreManager CreateInstance(
            DTE dte = null,
            ISolutionManager solutionManager = null,
            IFileSystemProvider fileSystemProvider = null,
            IVsPackageManagerFactory packageManagerFactory = null,
            IPackageRepositoryFactory packageRepositoryFactory = null,
            IVsThreadedWaitDialogFactory waitDialogFactory = null) {

            if (dte == null) {
                dte = new Mock<DTE>().Object;
            }

            if (solutionManager == null) {
                solutionManager = new Mock<ISolutionManager>().Object;
            }

            if (fileSystemProvider == null) {
                fileSystemProvider = new Mock<IFileSystemProvider>().Object;
            }

            if (packageManagerFactory == null) {
                packageManagerFactory = new Mock<IVsPackageManagerFactory>().Object;
            }

            if (packageRepositoryFactory == null) {
                packageRepositoryFactory = new Mock<IPackageRepositoryFactory>().Object;
            }

            if (waitDialogFactory == null) {
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

            return new PackageRestoreManager(
                dte,
                solutionManager,
                fileSystemProvider,
                packageManagerFactory,
                packageRepositoryFactory,
                waitDialogFactory);
        }

        private string CreateTempFolder() {
            string folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }
    }
}