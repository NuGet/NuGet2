using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace NuPack.Test {
    [TestClass]
    public class PathResolverTest {
        private const string _basePath = @"X:\abc\efg";

        [TestMethod]
        public void PathWithLocalFileReturnSingleResult() {
            // Arrange
            var path = "foo.txt";
            var expectedFilter = new PathSearchFilter(_basePath, "foo.txt", SearchOption.TopDirectoryOnly);

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithLocalFileAndLeadingDirectorySepReturnSingleResult() {
            // Arrange
            var path = "\\foo.txt";
            var expectedFilter = new PathSearchFilter(_basePath, "foo.txt", SearchOption.TopDirectoryOnly);

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RelativeFilePathReturnSingleResult() {
            // Arrange
            var path = "..\\..\\foo.txt";
            var directory = Path.GetDirectoryName(Path.GetDirectoryName(_basePath));
            var expectedFilter = new PathSearchFilter(directory, "foo.txt", SearchOption.TopDirectoryOnly);

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }


        [TestMethod]
        public void RootedFilePathReturnSingleResult() {
            // Arrange
            var path = "Y:\\foo\\bar.txt";
            var expectedFilter = new PathSearchFilter("Y:\\foo", "bar.txt", SearchOption.TopDirectoryOnly);

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithSingleWildCard() {
            // Arrange
            var path = "*";
            var expectedFilter = new PathSearchFilter(_basePath, "*", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardFileName() {
            // Arrange
            var path = "*.foo";
            var expectedFilter = new PathSearchFilter(_basePath, "*.foo", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardExtension() {
            // Arrange
            var path = "jquery.*";
            var expectedFilter = new PathSearchFilter(_basePath, "jquery.*", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardFileNameAndExtension() {
            // Arrange
            var path = "*.*";
            var expectedFilter = new PathSearchFilter(_basePath, "*.*", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithLeadingDirectorySepDirectoryAndWildCardFileNameAndExtension() {
            // Arrange
            var path = "\\foo\\*";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "foo"), "*", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithDirectoryAndWildCardFileNameAndExtension() {
            // Arrange
            var path = "foo\\*";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "foo"), "*", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithDirectoryAndWildCardFileName() {
            // Arrange
            var path = "\\bar\\foo\\*.baz";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "bar\\foo"), "*.baz", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RelativePathWildCardExtension() {
            // Arrange
            var path = "..\\..\\bar\\baz\\*.foo";
            var directory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_basePath)), "bar\\baz");
            var expectedFilter = new PathSearchFilter(directory, "*.foo", SearchOption.TopDirectoryOnly);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearch() {
            // Arrange
            var path = @"foo\**\.bar";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "foo"), ".bar", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingDirectoryStructure() {
            // Arrange
            var path = @"foo\**\baz\.bar";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "foo"), "baz\\.bar", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingNoExtension() {
            // Arrange
            var path = @"foo\**\*";
            var expectedFilter = new PathSearchFilter(Path.Combine(_basePath, "foo"), "*", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingNoLeadingDirectory() {
            // Arrange
            var path = @"**\*.txt";
            var expectedFilter = new PathSearchFilter(_basePath, "*.txt", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithOnlyRecursiveWildCardSearchChars() {
            // Arrange
            var path = @"**";
            var expectedFilter = new PathSearchFilter(_basePath, "*", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithInvalidRecursiveWildCardSearch() {
            // Arrange
            var path = @"**foo/bar";
            var expectedFilter = new PathSearchFilter(_basePath, "foo/bar", SearchOption.AllDirectories);

            // Act
            var searchFilter = PathResolver.ResolvePath(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void DestinationPathResolverGeneratesRelativePaths() {
            // Arrange
            var path = Path.GetFullPath(@".\foo\bar.txt");
            var basePath = String.Empty;

            // Act
            var result = PathResolver.ResolvePackagePath("*.*", basePath, path, String.Empty);

            // Assert
            Assert.AreEqual(@"foo\bar.txt", result);
        }

        [TestMethod]
        public void DestinationPathResolverGeneratesRelativePathsPrependedWithTargetPath() {
            // Arrange
            var path = Path.GetFullPath(@".\foo\bar.txt");
            var basePath = String.Empty;
            var targetPath = @"\abc\cdf";

            // Act
            var result = PathResolver.ResolvePackagePath("*.*", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(Path.Combine(targetPath, @"foo\bar.txt"), result);
        }

        [TestMethod]
        public void DestinationPathResolverReturnsFileNamesForNonRelativePaths() {
            // Arrange
            var searchPath = @"z:\bar\something.txt";
            var path = Path.GetFullPath(searchPath);
            var basePath = String.Empty;

            // Act
            var result = PathResolver.ResolvePackagePath(searchPath, basePath, path, String.Empty);

            // Assert
            Assert.AreEqual(@"something.txt", result);
        }

        [TestMethod]
        public void DestinationPathResolverReturnsFileNamesForPathsInBasePath() {
            // Arrange
            var path = Path.GetFullPath(@".\something.txt");
            var basePath = Path.GetFullPath(".");

            // Act
            var result = PathResolver.ResolvePackagePath("*.*", basePath, path, String.Empty);

            // Assert
            Assert.AreEqual(@"something.txt", result);
        }

        [TestMethod]
        public void DestinationPathResolverPrependsTargetPath() {
            // Arrange
            var path = Path.GetFullPath(@".\something.txt");
            var basePath = Path.GetFullPath(".");
            var targetPath = "foo";
            // Act
            var result = PathResolver.ResolvePackagePath("*.*", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(Path.Combine(targetPath, "something.txt"), result);
        }

        [TestMethod]
        public void PathResolverTruncatesRecursiveWildCardInSearchPathWhenNoTargetPathSpecified() {
            // Arrange
            var path = Path.GetFullPath(@"folder\sub-folder\somefile.txt");
            var basePath = Path.GetFullPath(".");
            var targetPath = String.Empty;
            
            // Act
            var result = PathResolver.ResolvePackagePath(@"folder\**", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(@"sub-folder\somefile.txt", result);
        }

        [TestMethod]
        public void PathResolverTruncatesRecursiveWildCardInSearchPathWhenTargetPathSpecified() {
            // Arrange
            var path = Path.GetFullPath(@"bin\debug\pack.dll");
            var basePath = Path.GetFullPath(".");
            var targetPath = @"lib\sl4";
            
            // Act
            var result = PathResolver.ResolvePackagePath(@"bin\debug\**", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(@"lib\sl4\pack.dll", result);
        }

        [TestMethod]
        public void PathResolverTruncatesWildCardInSearchPathWhenNoTargetPathSpecified() {
            // Arrange
            var path = Path.GetFullPath(@"bin\debug\pack.dll");
            var basePath = Path.GetFullPath(".");
            var targetPath = String.Empty;

            // Act
            var result = PathResolver.ResolvePackagePath(@"bin\debug\*.dll", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(@"pack.dll", result);
        }

        [TestMethod]
        public void PathResolverTruncatesWildCardInSearchPathWhenTargetPathSpecified() {
            // Arrange
            var path = Path.GetFullPath(@"bin\debug\pack.dll");
            var basePath = Path.GetFullPath(".");
            var targetPath = @"lib\sl4";

            // Act
            var result = PathResolver.ResolvePackagePath(@"bin\debug\*.dll", basePath, path, targetPath);

            // Assert
            Assert.AreEqual(@"lib\sl4\pack.dll", result);
        }

        private void AssertEqual(PathSearchFilter expected, PathSearchFilter actual) {
            Assert.AreEqual(expected.SearchDirectory, actual.SearchDirectory);
            Assert.AreEqual(expected.SearchOption, actual.SearchOption);
            Assert.AreEqual(expected.SearchPattern, actual.SearchPattern);
        }
    }
}
