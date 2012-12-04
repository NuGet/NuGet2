using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class DeleteOnRestartManagerTest
    {
        // This should match the constant of the same name in the DeleteOnRestartManager.
        // REVIEW: Should I just make this internal or public in DeleteOnRestartManager?
        private const string _deletionMarkerSuffix = ".deleteme";

        [Fact]
        public void PackageDirectoriesAreMarkedForDeletionIsTrueWhenDeletemeFileInRootOfPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.CreateDirectory("A.1.0.0");
            fileSystem.AddFile("A.1.0.0.deleteme");

            var deleteOnRestartManager = new DeleteOnRestartManager(() => fileSystem);

            // Assert
            Assert.True(deleteOnRestartManager.GetPackageDirectoriesMarkedForDeletion().Any());
        }

        [Fact]
        public void PackageDirectoriesAreMarkedForDeletionIsFalsesWhenNoDeletemeFileInRootOfPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.CreateDirectory("A.1.0.0");
            fileSystem.AddFile("A.1.0.0");

            var deleteOnRestartManager = new DeleteOnRestartManager(() => fileSystem);

            // Act
            // Assert
            Assert.False(deleteOnRestartManager.GetPackageDirectoriesMarkedForDeletion().Any());
        }

        [Fact]
        public void MarkPackageDirectoryForDeletionDoesNotAddDeletemeFileWhenDirectoryRemovalSuccessful()
        {
            // Arrange
            IPackage package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0.0");
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            string packageDirectoryPath = pathResolver.GetPackageDirectory(package);

            var deleteOnRestartManager = new DeleteOnRestartManager(() => fileSystem, () => pathResolver);

            // Act
            deleteOnRestartManager.MarkPackageDirectoryForDeletion(package);

            // Assert
            Assert.False(fileSystem.DirectoryExists(packageDirectoryPath));
            Assert.False(fileSystem.FileExists(packageDirectoryPath + _deletionMarkerSuffix));
        }

        [Fact]
        public void MarkPackageDirectoryForDeletionAddsDeletemeFileWhenDirectoryRemovalUnsuccessful()
        {
            // Arrange
            IPackage package = NuGet.Test.PackageUtility.CreatePackage(id: "foo", version: "1.0.0", content: new string[] { }, assemblyReferences: new string[] { }, tools: new[] { "lockedFile.txt" });
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            string packageDirectoryPath = pathResolver.GetPackageDirectory(package);
            string lockedFilePath = Path.Combine("tools", "lockedFile.txt");

            fileSystem.AddFile(Path.Combine(packageDirectoryPath, pathResolver.GetPackageFileName(package)), Stream.Null);
            fileSystem.AddFile(Path.Combine(packageDirectoryPath, packageDirectoryPath + Constants.ManifestExtension), Stream.Null);
            fileSystem.AddFile(Path.Combine(packageDirectoryPath, lockedFilePath), lockedFilePath.AsStream());

            var deleteOnRestartManager = new DeleteOnRestartManager(() => fileSystem, () => pathResolver);

            // Act
            deleteOnRestartManager.MarkPackageDirectoryForDeletion(package);

            // Assert
            Assert.True(fileSystem.DirectoryExists(packageDirectoryPath));
            Assert.True(fileSystem.FileExists(packageDirectoryPath + _deletionMarkerSuffix));
        }

        [Fact]
        public void DeleteMarkedPackageDirectoriesRemovesDirectoriesAndAssociatedDeletemeFiles()
        {
            // Arrange
            IPackage packageA = NuGet.Test.PackageUtility.CreatePackage("A", "1.0.0");
            IPackage packageB = NuGet.Test.PackageUtility.CreatePackage("B", "1.0.0");

            var fileSystemProvider = new MockFileSystemProvider();
            var fileSystem = fileSystemProvider.GetFileSystem("packages");

            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            string packageADirectory = pathResolver.GetPackageDirectory(packageA);
            string packageBDirectory = pathResolver.GetPackageDirectory(packageB);
            string packageAFilePath = Path.Combine(packageADirectory, pathResolver.GetPackageFileName(packageA));
            string packageBFilePath = Path.Combine(packageBDirectory, pathResolver.GetPackageFileName(packageB));

            var mockRepositorySettings = new Mock<IRepositorySettings>();
            mockRepositorySettings.SetupGet(m => m.RepositoryPath).Returns("packages");

            fileSystem.AddFile(packageAFilePath, Stream.Null);
            fileSystem.AddFile(packageBFilePath, Stream.Null);
            fileSystem.AddFile(packageADirectory + _deletionMarkerSuffix, Stream.Null);

            var deleteOnRestartManager = new DeleteOnRestartManager(() => fileSystemProvider.GetFileSystem(mockRepositorySettings.Object.RepositoryPath),
                                                                    () => pathResolver);

            // Act
            deleteOnRestartManager.DeleteMarkedPackageDirectories();

            // Assert
            Assert.False(fileSystem.FileExists(packageAFilePath));
            Assert.False(fileSystem.DirectoryExists(packageADirectory));
            Assert.False(fileSystem.FileExists(packageADirectory + _deletionMarkerSuffix));
            Assert.True(fileSystem.FileExists(packageBFilePath));
            Assert.True(fileSystem.DirectoryExists(packageBDirectory));
            Assert.False(fileSystem.FileExists(packageBDirectory + _deletionMarkerSuffix));
        }

        private class MockFileSystemShallowCopy : MockFileSystem
        {
            // GetFiles must shallow copy in order to allow SolutionManager.CleanUpDeletedPackageDirectories() to modify 
            // the file system via DeleteDirectory and DeleteFile while enumerating the return value of GetFiles.
            public override IEnumerable<string> GetFiles(string path, bool recursive)
            {
                return base.GetFiles(path, recursive).ToList();
            }
        }

        private class MockFileSystemProvider : IFileSystemProvider
        {
            private IFileSystem _mockFileSystem = new MockFileSystemShallowCopy();

            public IFileSystem GetFileSystem(string path)
            {
                return GetFileSystem(path, ignoreSourceControlSetting: false);
            }

            public IFileSystem GetFileSystem(string path, bool ignoreSourceControlSetting)
            {
                return _mockFileSystem;
            }
        }
    }
}