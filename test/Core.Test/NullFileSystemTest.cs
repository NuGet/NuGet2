using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class NullFileSystemTest {
        [TestMethod]
        public void NullFileSystemReturnsNoFilesOrDirectories() {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act
            var files = instance.GetFiles("/foo");
            var directories = instance.GetDirectories("/bar");
            var filteredFiles = instance.GetFiles("/foo", "*.txt");

            // Assert
            Assert.IsFalse(files.Any());
            Assert.IsFalse(directories.Any());
            Assert.IsFalse(filteredFiles.Any());
        }

        [TestMethod]
        public void NullFileSystemReturnsFalseForExistenceChecks() {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            Assert.IsFalse(instance.FileExists("foo.txt"));
            Assert.IsFalse(instance.FileExists("bar.txt"));
        }

        [TestMethod]
        public void NullFileSystemReturnsNullStreamForOpen() {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            Assert.AreEqual(Stream.Null, instance.OpenFile("foo.txt"));
        }

        [TestMethod]
        public void NullFileSystemDoesNotThrowForFileOperations() {
            // Arrange
            var instance = NullFileSystem.Instance;

            // Act and Assert
            instance.DeleteDirectory("foo", recursive: false);
            instance.DeleteDirectory("foo", recursive: true);
            instance.DeleteFile("foo.txt");
            instance.AddFile("foo", "Hello world".AsStream());

            // If we've come this far, no exceptions were thrown.
            Assert.IsTrue(true);
        }
    }
}
