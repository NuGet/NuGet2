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
            Assert.Equal<string>("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsTheEmptyString()
        {
            var exception = Assert.Throws<ArgumentException>(() => new PhysicalFileSystem(string.Empty));
            Assert.Equal<string>("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorInitializesInstance()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            
            var target = new PhysicalFileSystem(root);

            Assert.Equal<string>(root, target.Root, StringComparer.Ordinal);
        }

        [Fact]
        public void GetFullPathCombinesRootAndSpecifiedPath()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var path = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            string result = target.GetFullPath(path);

            Assert.Equal<string>(Path.Combine(root, path), result, StringComparer.Ordinal);
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfStreamIsNull()
        {
            var root = Path.GetRandomFileName();
            var target = new PhysicalFileSystem(root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.AddFile(Path.GetRandomFileName(), null));
            Assert.Equal<string>("stream", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void GetFullPathThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new PhysicalFileSystem(root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.GetFullPath(null));
            Assert.Equal<string>("path", exception.ParamName, StringComparer.Ordinal);
        }
    }
}