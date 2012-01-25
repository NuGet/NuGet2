using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Test
{

    public class NullFileSystemTest
    {
        [Fact]
        public void NullFileSystemReturnsNoFilesOrDirectories()
        {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act
            var files = instance.GetFiles("/foo", "*.*");
            var directories = instance.GetDirectories("/bar");
            var filteredFiles = instance.GetFiles("/foo", "*.txt");

            // Assert
            Assert.False(files.Any());
            Assert.False(directories.Any());
            Assert.False(filteredFiles.Any());
        }

        [Fact]
        public void NullFileSystemReturnsFalseForExistenceChecks()
        {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            Assert.False(instance.FileExists("foo.txt"));
            Assert.False(instance.FileExists("bar.txt"));
        }

        [Fact]
        public void NullFileSystemReturnsNullStreamForOpen()
        {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            Assert.Equal(Stream.Null, instance.OpenFile("foo.txt"));
        }

        [Fact]
        public void NullFileSystemDoesNotThrowForFileOperations()
        {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            instance.DeleteDirectory("foo", recursive: false);
            instance.DeleteDirectory("foo", recursive: true);
            instance.DeleteFile("foo.txt");
            instance.AddFile("foo", "Hello world".AsStream());

            // If we've come this far, no exceptions were thrown.
            Assert.True(true);
        }
    }
}