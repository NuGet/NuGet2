using System;
using System.IO;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PhysicalFileSystemTest
    {
        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsNull()
        {
            // Act and Assert
            var exception = Assert.Throws<ArgumentException>(() => new PhysicalFileSystem(null));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsTheEmptyString()
        {
            // Act and Assert
            var exception = Assert.Throws<ArgumentException>(() => new PhysicalFileSystem(string.Empty));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorInitializesInstance()
        {
            // Arrange
            var root = @"X:\MyRoot\MyDir";
            
            // Act
            var target = new PhysicalFileSystem(root);

            // Assert
            Assert.Equal(root, target.Root);
        }

        [Fact]
        public void GetFullPathCombinesRootAndSpecifiedPath()
        {
            // Arrange
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var path = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            // Act
            string result = target.GetFullPath(path);

            // Assert
            Assert.Equal(Path.Combine(root, path), result, StringComparer.Ordinal);
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfStreamIsNull()
        {
            // Arrange
            var root = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => target.AddFile(Path.GetRandomFileName(), stream: null), "stream");
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfWriteToStreamIsNull()
        {
            // Arrange
            var root = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => target.AddFile(Path.GetRandomFileName(), writeToStream: null), "writeToStream");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetFullPathReturnsRootIfPathIsNullOrEmpty(string path)
        {
            // Arrange
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new PhysicalFileSystem(root);

            // Act
            var fullPath = target.GetFullPath(path);

            // Assert
            Assert.Equal(root, fullPath);
        }
    }
}