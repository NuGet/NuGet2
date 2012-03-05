using System.IO;
using Xunit;

namespace NuGet.Test
{

    public class PathUtilityTest
    {
        [Fact]
        public void EnsureTrailingSlashThrowsIfPathIsNull()
        {
            // Arrange
            string path = null;

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => PathUtility.EnsureTrailingSlash(path), "path");
        }

        [Fact]
        public void EnsureTrailingSlashReturnsOriginalPathIfEmpty()
        {
            // Arrange
            string path = "";

            // Act
            string output = PathUtility.EnsureTrailingSlash(path);

            // Assert
            Assert.Equal(path, output);
        }

        [Fact]
        public void EnsureTrailingSlashReturnsOriginalStringIfPathTerminatesInSlash()
        {
            // Arrange
            string path = @"foo\bar\";

            // Act
            string output = PathUtility.EnsureTrailingSlash(path);

            // Assert
            Assert.Equal(path, output);
        }

        [Fact]
        public void EnsureTrailingSlashAppendsSlashIfPathDoesNotTerminateInSlash()
        {
            // Arrange
            string path1 = @"foo\bar";
            string path2 = "foo";

            // Act
            string output1 = PathUtility.EnsureTrailingSlash(path1);
            string output2 = PathUtility.EnsureTrailingSlash(path2);

            // Assert
            Assert.Equal(path1 + Path.DirectorySeparatorChar, output1);
            Assert.Equal(path2 + Path.DirectorySeparatorChar, output2);
        }

        [Fact]
        public void GetRelativePathAbsolutePaths()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar\", @"c:\foo\bar\baz");

            // Assert
            Assert.Equal("baz", path);
        }

        [Fact]
        public void GetRelativePathDirectoryWithPeriods()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\MvcApplication1\MvcApplication1.Tests\", @"c:\foo\MvcApplication1\packages\foo.dll");

            // Assert
            Assert.Equal(@"..\packages\foo.dll", path);
        }

        [Fact]
        public void GetRelativePathAbsolutePathAndShare()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"\\baz");

            // Assert
            Assert.Equal(@"\\baz", path);
        }

        [Fact]
        public void GetRelativePathShares()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"\\baz\a\b\c\", @"\\baz\");

            // Assert
            Assert.Equal(@"..\..\..\", path);
        }

        [Fact]
        public void GetRelativePathFileNames()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\a\y\x.dll", @"c:\a\b.dll");

            // Assert
            Assert.Equal(@"..\b.dll", path);
        }

        [Fact]
        public void GetRelativePathWithSpaces()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"c:\foo\This is a folder");

            // Assert
            Assert.Equal(@"foo\This is a folder", path);
        }

        [Fact]
        public void GetAbsolutePathWithSpaces()
        {
            // Act
            string path = PathUtility.GetAbsolutePath(@"c:\foo\", @"This is a folder");

            // Assert
            Assert.Equal(@"c:\foo\This is a folder", path);
        }

        [Fact]
        public void GetRelativePathUnrelatedAbsolutePaths()
        {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"d:\bar");

            // Assert
            Assert.Equal(@"d:\bar", path);
        }

        [Fact]
        public void GetAbsolutePathComplementsGetRelativePath()
        {
            // Arrange
            string basePath = @"c:\foo\bar\baz";
            string targetPath = @"c:\foo";
            string relativePath = PathUtility.GetRelativePath(basePath, targetPath);

            // Act
            string absolutePath = PathUtility.GetAbsolutePath(basePath, relativePath);

            // Assert
            Assert.Equal(targetPath, absolutePath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectLocalPathWithTrailingBackslash()
        {
            // Arrange
            string basePath = @"c:\foo\bar\baz";
            string canonicalPath = @"c:\foo\bar\baz\";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectLocalPathWithoutMultipleBackslashes()
        {
            // Arrange
            string basePath = @"c:\\foo\\bar\\baz";
            string canonicalPath = @"c:\foo\bar\baz\";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUncPathWithTrailingBackslash()
        {
            // Arrange
            string basePath = @"\\server\share\foo\bar\baz";
            string canonicalPath = @"\\server\share\foo\bar\baz\";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUncPathWithoutMultipleBackslashes()
        {
            // Arrange
            string basePath = @"\\server\\share\\foo\\bar\\baz";
            string canonicalPath = @"\\server\share\foo\bar\baz\";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlForDomainOnlyWithTrailingSlash()
        {
            // Arrange
            string basePath = @"http://www.example.com";
            string canonicalPath = @"http://www.example.com/";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlForFolderWithoutTrailingSlash()
        {
            // Arrange
            string basePath = @"http://www.example.com/nuget";
            string canonicalPath = @"http://www.example.com/nuget";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlForFolderWithTrailingSlash()
        {
            // Arrange
            string basePath = @"http://www.example.com/nuget/";
            string canonicalPath = @"http://www.example.com/nuget/";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlWithFilename()
        {
            // Arrange
            string basePath = @"http://www.example.com/nuget/index.html";
            string canonicalPath = @"http://www.example.com/nuget/index.html";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlWithQueryString()
        {
            // Arrange
            string basePath = @"http://www.example.com/nuget/index.html?abc=123";
            string canonicalPath = @"http://www.example.com/nuget/index.html?abc=123";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(targetPath, canonicalPath);
        }

        [Fact]
        public void GetCanonicalPathReturnsCorrectUrlWithEncodedQueryString()
        {
            // Arrange
            string basePath = @"http://www.example.com/nuget/index.html?abc%3D123";
            string canonicalPath = @"http://www.example.com/nuget/index.html?abc%3D123";

            // Act
            string targetPath = PathUtility.GetCanonicalPath(basePath);

            // Assert
            Assert.Equal(canonicalPath, targetPath);
        }
    }
}