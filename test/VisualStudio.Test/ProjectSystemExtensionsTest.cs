using System;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class ProjectSystemExtensionsTest
    {
        [Fact]
        public void CreateRefreshFileAddsRefreshFileUnderBinDirectory()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(VersionUtility.DefaultTargetFramework, @"x:\test\site\");
            var assemblyPath = @"x:\test\packages\Foo.1.0\lib\net40\Foo.dll";

            // Act
            projectSystem.CreateRefreshFile(assemblyPath);

            // Assert
            Assert.Equal(@"..\packages\Foo.1.0\lib\net40\Foo.dll", projectSystem.ReadAllText(@"bin\Foo.dll.refresh"));
        }

        [Fact]
        public void CreateRefreshFileUsesAbsolutePathIfRelativePathsCannotBeFormed()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(VersionUtility.DefaultTargetFramework, @"z:\test\site\");
            var assemblyPath = @"x:\test\packages\Foo.1.0\lib\net40\Bar.net40.dll";

            // Act
            projectSystem.CreateRefreshFile(assemblyPath);

            // Assert
            Assert.Equal(@"x:\test\packages\Foo.1.0\lib\net40\Bar.net40.dll", projectSystem.ReadAllText(@"bin\Bar.net40.dll.refresh"));
        }

        [Fact]
        public void ResolveRefreshPathsReturnsEmptyCollectionIfNoRefreshFilesAreFoundInBin()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            
            // Act
            var resolvedPaths = fileSystem.ResolveRefreshPaths();

            // Assert
            Assert.Empty(resolvedPaths);
        }

        [Fact]
        public void ResolvedRefreshPathsIgnoresUnreadableFiles()
        {
            // Arrange
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystem.Setup(f => f.GetFiles("bin", "*.refresh", false)).Returns(new[] { @"bin\Foo.refresh", @"bin\Bar.refresh" });
            fileSystem.Setup(f => f.OpenFile(@"bin\Foo.refresh")).Throws(new Exception("Can't read this"));
            fileSystem.Setup(f => f.OpenFile(@"bin\Bar.refresh")).Returns(@"..\..\packages\Bar.1.0\lib\net40\bar.dll".AsStream());
            fileSystem.Setup(f => f.GetFullPath(@"..\..\packages\Bar.1.0\lib\net40\bar.dll")).Returns("Bar.dll");
            fileSystem.Setup(f => f.FileExists("Bar.dll")).Returns(true);

            // Act
            var resolvedPaths = fileSystem.Object.ResolveRefreshPaths();

            // Assert
            Assert.Equal(new[] { "Bar.dll" }, resolvedPaths);
        }

        [Fact]
        public void ResolvedRefreshPathsIgnoresFilesThatDoNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bin\Foo.refresh", "x:\foo.dll");
            fileSystem.AddFile(@"bin\bar.refresh", "bar.dll");
            fileSystem.AddFile(@"C:\MockFileSystem\bar.dll");

            // Act
            var resolvedPaths = fileSystem.ResolveRefreshPaths();

            // Assert
            Assert.Equal(new[] { @"C:\MockFileSystem\bar.dll" }, resolvedPaths);
        }

        [Fact]
        public void ResolvedRefreshPathsIgnoresPathsThatAreNotAssemblyReferences()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bin\Foo.refresh", "foo.dll");
            fileSystem.AddFile(@"bin\qux.refresh", "qux.exe");
            fileSystem.AddFile(@"bin\bar.refresh", "bar.pdb");
            fileSystem.AddFile(@"C:\MockFileSystem\foo.dll");
            fileSystem.AddFile(@"C:\MockFileSystem\qux.exe");

            // Act
            var resolvedPaths = fileSystem.ResolveRefreshPaths();

            // Assert
            Assert.Equal(new[] { @"C:\MockFileSystem\foo.dll", @"C:\MockFileSystem\qux.exe" }, resolvedPaths);
        }
    }
}
