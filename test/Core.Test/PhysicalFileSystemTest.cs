using System;
using System.IO;
using Xunit;

namespace NuGet.Test
{
    public class PhysicalFileSystemTest
    {
        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsNull()
        {
            var exception = Assert.Throws<ArgumentException>(() => new PhysicalFileSystem(null));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsTheEmptyString()
        {
            var exception = Assert.Throws<ArgumentException>(() => new PhysicalFileSystem(string.Empty));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorInitializesInstance()
        {
            var root = @"X:\MyRoot\MyDir";
            
            var target = new PhysicalFileSystem(root);

            Assert.Equal(root, target.Root, StringComparer.Ordinal);
        }

        [Fact]
        public void GetFullPathCombinesRootAndSpecifiedPath()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var path = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            string result = target.GetFullPath(path);

            Assert.Equal(Path.Combine(root, path), result, StringComparer.Ordinal);
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfStreamIsNull()
        {
            var root = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            ExceptionAssert.ThrowsArgNull(() => target.AddFile(Path.GetRandomFileName(), null), "stream");
        }

        [Fact]
        public void GetFullPathThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new PhysicalFileSystem(root);

            ExceptionAssert.ThrowsArgNull(() => target.GetFullPath(null), "path");
        }
    }
}