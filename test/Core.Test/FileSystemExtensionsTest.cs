using System;
using System.IO;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class FileSystemExtensionsTest
    {
        [Fact]
        public void AddFilesAddFilesToProjectSystem()
        {
            // Arrange
            var fileSystem = new MockProjectSystem();
            var files = PackageUtility.CreateFiles(new[] { "A", "B", "C" });

            // Act
            fileSystem.AddFiles(files, String.Empty);

            // Assert
            Assert.True(fileSystem.FileExists("A"));
            Assert.True(fileSystem.FileExists("B"));
            Assert.True(fileSystem.FileExists("C"));
        }

        [Fact]
        public void AddFilesAddFilesToProjectSystemIfNotExists()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.Logger).Returns(NullLogger.Instance);
            mockFileSystem.Setup(m => m.AddFile(It.IsAny<string>(), It.IsAny<Stream>())).Verifiable();
            mockFileSystem.Setup(m => m.FileExists("A")).Returns(true);
            var files = PackageUtility.CreateFiles(new[] { "A", "B", "C" });

            // Act
            mockFileSystem.Object.AddFiles(files, String.Empty);

            // Assert
            mockFileSystem.Verify(m => m.AddFile("A", It.IsAny<Stream>()), Times.Never());
        }

        [Fact]
        public void DeleteFileAndEmptyParentDirectoriesCorrectly()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("foo\\bar\\hell\\x.txt");

            // Act
            fileSystem.DeleteFileAndParentDirectoriesIfEmpty("foo\\bar\\hell\\x.txt");

            // Assert
            Assert.False(fileSystem.FileExists("foo\\bar\\hell\\x.txt"));
            Assert.False(fileSystem.DirectoryExists("foo"));
            Assert.False(fileSystem.DirectoryExists("foo\\bar"));
            Assert.False(fileSystem.DirectoryExists("foo\\bar\\hell"));
        }

        [Fact]
        public void DeleteFileAndEmptyParentDirectoriesDoNotDeleteDirectoryIfNotEmpty()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("foo\\bar\\hell\\x.txt");
            fileSystem.AddFile("foo\\bar\\hell\\y.txt");

            // Act
            fileSystem.DeleteFileAndParentDirectoriesIfEmpty("foo\\bar\\hell\\x.txt");

            // Assert
            Assert.False(fileSystem.FileExists("foo\\bar\\hell\\x.txt"));
            Assert.True(fileSystem.DirectoryExists("foo"));
            Assert.True(fileSystem.DirectoryExists("foo\\bar"));
            Assert.True(fileSystem.DirectoryExists("foo\\bar\\hell"));
        }

        [Fact]
        public void DeleteFileAndEmptyParentDirectoriesDoNotDeleteParentDirectoryIfNotEmpty()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            fileSystem.AddFile("foo\\bar\\hell\\x.txt");
            fileSystem.AddFile("foo\\bar\\y.txt");

            // Act
            fileSystem.DeleteFileAndParentDirectoriesIfEmpty("foo\\bar\\hell\\x.txt");

            // Assert
            Assert.False(fileSystem.FileExists("foo\\bar\\hell\\x.txt"));
            Assert.True(fileSystem.DirectoryExists("foo"));
            Assert.True(fileSystem.DirectoryExists("foo\\bar"));
            Assert.False(fileSystem.DirectoryExists("foo\\bar\\hell"));
        }
    }
}
