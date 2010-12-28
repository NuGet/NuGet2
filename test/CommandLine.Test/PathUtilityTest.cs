using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathUtilityTest {
        [TestMethod]
        public void EnsureTrailingSlashThrowsIfPathIsNull() {
            // Arrange
            string path = null;

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => PathUtility.EnsureTrailingSlash(path), "path");
        }

        [TestMethod]
        public void EnsureTrailingSlashReturnsOriginalPathIfEmpty() {
            // Arrange
            string path = "";

            // Act
            string output = PathUtility.EnsureTrailingSlash(path);

            // Assert
            Assert.AreEqual(path, output);
        }

        [TestMethod]
        public void EnsureTrailingSlashReturnsOriginalStringIfPathTerminatesInSlash() {
            // Arrange
            string path = @"foo\bar\";

            // Act
            string output = PathUtility.EnsureTrailingSlash(path);

            // Assert
            Assert.AreEqual(path, output);
        }

        [TestMethod]
        public void EnsureTrailingSlashAppendsSlashIfPathDoesNotTerminateInSlash() {
            // Arrange
            string path1 = @"foo\bar";
            string path2 = "foo";

            // Act
            string output1 = PathUtility.EnsureTrailingSlash(path1);
            string output2 = PathUtility.EnsureTrailingSlash(path2);

            // Assert
            Assert.AreEqual(path1 + Path.DirectorySeparatorChar, output1);
            Assert.AreEqual(path2 + Path.DirectorySeparatorChar, output2);
        }

        [TestMethod]
        public void GetRelativePathAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar\", @"c:\foo\bar\baz");

            // Assert
            Assert.AreEqual("baz", path);
        }

        [TestMethod]
        public void GetRelativePathDirectoryWithPeriods() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\MvcApplication1\MvcApplication1.Tests\", @"c:\foo\MvcApplication1\packages\foo.dll");

            // Assert
            Assert.AreEqual(@"..\packages\foo.dll", path);
        }

        [TestMethod]
        public void GetRelativePathAbsolutePathAndShare() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"\\baz");

            // Assert
            Assert.AreEqual(@"\\baz", path);
        }

        [TestMethod]
        public void GetRelativePathShares() {
            // Act
            string path = PathUtility.GetRelativePath(@"\\baz\a\b\c\", @"\\baz\");

            // Assert
            Assert.AreEqual(@"..\..\..\", path);
        }

        [TestMethod]
        public void GetRelativePathFileNames() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\a\y\x.dll", @"c:\a\b.dll");

            // Assert
            Assert.AreEqual(@"..\b.dll", path);
        }

        [TestMethod]
        public void GetRelativePathWithSpaces() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"c:\foo\This is a folder");

            // Assert
            Assert.AreEqual(@"foo\This is a folder", path);
        }

        [TestMethod]
        public void GetAbsolutePathWithSpaces() {
            // Act
            string path = PathUtility.GetAbsolutePath(@"c:\foo\", @"This is a folder");

            // Assert
            Assert.AreEqual(@"c:\foo\This is a folder", path);
        }

        [TestMethod]
        public void GetRelativePathUnrelatedAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"d:\bar");

            // Assert
            Assert.AreEqual(@"d:\bar", path);
        }

        [TestMethod]
        public void GetAbsolutePathComplementsGetRelativePath() {
            // Arrange
            string basePath = @"c:\foo\bar\baz";
            string targetPath = @"c:\foo";
            string relativePath = PathUtility.GetRelativePath(basePath, targetPath);

            // Act
            string absolutePath = PathUtility.GetAbsolutePath(basePath, relativePath);

            // Assert
            Assert.AreEqual(targetPath, absolutePath);
        }
    }
}
