using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathResolverTest {
        private const string _basePath = @"X:\abc\efg";

        [TestMethod]
        public void PathWithLocalFileReturnSingleResult() {
            // Arrange
            var path = "foo.txt";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "foo.txt", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithLocalFileAndLeadingDirectorySepReturnSingleResult() {
            // Arrange
            var path = "\\foo.txt";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "foo.txt", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RelativeFilePathReturnSingleResult() {
            // Arrange
            var path = "..\\..\\foo.txt";
            var directory = Path.GetDirectoryName(Path.GetDirectoryName(_basePath));
            var expectedFilter = new PathSearchFilter { SearchDirectory = directory, SearchPattern = "foo.txt", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }


        [TestMethod]
        public void RootedFilePathReturnSingleResult() {
            // Arrange
            var path = "Y:\\foo\\bar.txt";
            var expectedFilter = new PathSearchFilter { SearchDirectory = "Y:\\foo", SearchPattern = "bar.txt", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act 
            PathSearchFilter searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithSingleWildCard() {
            // Arrange
            var path = "*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "*", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardFileName() {
            // Arrange
            var path = "*.foo";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "*.foo", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardExtension() {
            // Arrange
            var path = "jquery.*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "jquery.*", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithWildCardFileNameAndExtension() {
            // Arrange
            var path = "*.*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "*.*", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithLeadingDirectorySepDirectoryAndWildCardFileNameAndExtension() {
            // Arrange
            var path = "\\foo\\*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "foo"), SearchPattern = "*", SearchOption = SearchOption.TopDirectoryOnly };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithDirectoryAndWildCardFileNameAndExtension() {
            // Arrange
            var path = "foo\\*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "foo"), SearchPattern = "*", SearchOption = SearchOption.TopDirectoryOnly };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithDirectoryAndWildCardFileName() {
            // Arrange
            var path = "\\bar\\foo\\*.baz";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "bar\\foo"), SearchPattern = "*.baz", SearchOption = SearchOption.TopDirectoryOnly };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RelativePathWildCardExtension() {
            // Arrange
            var path = "..\\..\\bar\\baz\\*.foo";
            var directory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_basePath)), "bar\\baz");
            var expectedFilter = new PathSearchFilter { SearchDirectory = directory, SearchPattern = "*.foo", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearch() {
            // Arrange
            var path = @"foo\**\.bar";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "foo"), SearchPattern = ".bar", SearchOption = SearchOption.AllDirectories };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingDirectoryStructure() {
            // Arrange
            var path = @"foo\**\baz\.bar";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "foo"), SearchPattern = "baz\\.bar", SearchOption = SearchOption.AllDirectories };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingNoExtension() {
            // Arrange
            var path = @"foo\**\*";
            var expectedFilter = new PathSearchFilter { SearchDirectory = Path.Combine(_basePath, "foo"), SearchPattern = "*", SearchOption = SearchOption.AllDirectories };

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithRecursiveWildCardSearchContainingNoLeadingDirectory() {
            // Arrange
            var path = @"**\*.txt";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "*.txt", SearchOption = SearchOption.AllDirectories }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithOnlyRecursiveWildCardSearchChars() {
            // Arrange
            var path = @"**";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = "*", SearchOption = SearchOption.AllDirectories }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void PathWithInvalidRecursiveWildCardSearch() {
            // Arrange
            var path = @"**foo\bar";
            var expectedFilter = new PathSearchFilter { SearchDirectory = _basePath, SearchPattern = @"foo\bar", SearchOption = SearchOption.AllDirectories }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(_basePath, path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RootedPathWithoutWildCard() {
            // Arrange
            var path = @"x:\foo\bar\baz.cs";
            var expectedFilter = new PathSearchFilter { SearchDirectory = @"x:\foo\bar", SearchPattern = @"baz.cs", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(@"C:\project-files", path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RootedPathWithWildCard() {
            // Arrange
            var path = @"x:\foo\bar\*.cs";
            var expectedFilter = new PathSearchFilter { SearchDirectory = @"x:\foo\bar", SearchPattern = @"*.cs", SearchOption = SearchOption.TopDirectoryOnly }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(@"C:\project-files", path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void RootedPathWithRecursiveWildCard() {
            // Arrange
            var path = @"x:\foo\bar\**\*.cs";
            var expectedFilter = new PathSearchFilter { SearchDirectory = @"x:\foo\bar", SearchPattern = @"*.cs", SearchOption = SearchOption.AllDirectories }; ;

            // Act
            var searchFilter = PathResolver.ResolveSearchFilter(@"C:\project-files", path);

            // Assert
            AssertEqual(expectedFilter, searchFilter);
        }

        [TestMethod]
        public void DestinationPathResolverGeneratesRelativePaths() {
            // Arrange
            var path = @"root\sub-dir\foo\bar.txt";
            var basePath = @"root\sub-dir";

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = basePath, SearchPattern = "*.*", WildCardSearch = true }, path, String.Empty);

            // Assert
            Assert.AreEqual(@"foo\bar.txt", result);
        }

        [TestMethod]
        public void DestinationPathResolverGeneratesRelativePathsPrependedWithTargetPath() {
            // Arrange
            var path = @"dir\subdir\foo\bar.txt";
            var basePath = @"dir\subdir";
            var targetPath = @"\abc\cdf";

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = basePath, SearchPattern = "*.*", WildCardSearch = true }, path, targetPath);

            // Assert
            Assert.AreEqual(Path.Combine(targetPath, @"foo\bar.txt"), result);
        }

        [TestMethod]
        public void DestinationPathResolverReturnsFileNamesForPathsInBasePath() {
            // Arrange
            var path = @"dir\subdir\something.txt";
            var basePath = @"dir\subdir";

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = basePath, SearchPattern = "*.*", WildCardSearch = true }, path, String.Empty);

            // Assert
            Assert.AreEqual(@"something.txt", result);
        }

        [TestMethod]
        public void DestinationPathResolverPrependsTargetPath() {
            // Arrange
            var path = @"dir\subdir\something.txt";
            var basePath = @"dir\subdir";
            var targetPath = "foo";
            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = basePath, SearchPattern = "*.*", WildCardSearch = true }, path, targetPath);

            // Assert
            Assert.AreEqual(Path.Combine(targetPath, "something.txt"), result);
        }

        [TestMethod]
        public void PathResolverTruncatesRecursiveWildCardInSearchPathWhenNoTargetPathSpecified() {
            // Arrange
            var path = @"root\folder\sub-folder\somefile.txt";
            var searchDirectory = @"root\folder";
            var targetPath = String.Empty;

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = searchDirectory, SearchPattern = "*.txt", WildCardSearch = true }, path, targetPath);

            // Assert
            Assert.AreEqual(@"sub-folder\somefile.txt", result);
        }

        [TestMethod]
        public void PathResolverTruncatesRecursiveWildCardInSearchPathWhenTargetPathSpecified() {
            // Arrange
            var path = @"root\dir\subdir\pack.dll";
            var searchDirectory = @"root\dir";
            var targetPath = @"lib\sl4";

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = searchDirectory, SearchPattern = "*.*", WildCardSearch = true }, path, targetPath);

            // Assert
            Assert.AreEqual(@"lib\sl4\subdir\pack.dll", result);
        }

        [TestMethod]
        public void PathResolverUsesTargetPathWhenFileExtensionMatchesAndSearchPathDoesNotContainWildcard() {
            // Arrange
            var path = @"root\dir\subdir\foo.dll";
            var basePath = @"root";
            var targetPath = @"lib\sl4\bar.dll";

            // Act
            var result = PathResolver.ResolvePackagePath(new PathSearchFilter { SearchDirectory = basePath, SearchPattern = "foo.dll", WildCardSearch = false }, path, targetPath);

            // Assert
            Assert.AreEqual(targetPath, result);
        }

        [TestMethod]
        public void GetMatchesFiltersByWildCards() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"baz.pdb" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\*.txt", "*.pdb" });

            // Assert
            Assert.AreEqual(2, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"baz.pdb", matches.ElementAt(1).SourcePath);
        }

        [TestMethod]
        public void GetMatchesAllowsRecursiveWildcardMatches() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"lib\baz.pdb" },
                new PhysicalPackageFile { SourcePath = @"baz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\**\.txt", "**.pdb" });

            // Assert
            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.AreEqual(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [TestMethod]
        public void GetMatchesPerformsRecursiveWildcardSearch() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"lib\baz.pdb" },
                new PhysicalPackageFile { SourcePath = @"baz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\**\.txt", "**.pdb" });

            // Assert
            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.AreEqual(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [TestMethod]
        public void GetMatchesPerformsExactMatches() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"foo.dll" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo.dll" },
                new PhysicalPackageFile { SourcePath = @"bin\debug\baz.dll" },
                new PhysicalPackageFile { SourcePath = @"bin\debug\notbaz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"foo.dll", @"bin\*\b*.dll" });

            // Assert
            Assert.AreEqual(2, matches.Count());
            Assert.AreEqual(@"foo.dll", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"bin\debug\baz.dll", matches.ElementAt(1).SourcePath);
        }

        [TestMethod]
        public void FilterPathRemovesItemsThatMatchWildcard() {
            // Arrange
            var files = new List<IPackageFile>(new[] { 
                new PhysicalPackageFile { TargetPath = @"foo.dll" }, 
                new PhysicalPackageFile { TargetPath = @"content\foo.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\baz.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\notbaz.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\baz.pdb" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\notbaz.pdb" },
            });

            // Act
            PathResolver.FilterPackageFiles(files, f => f.Path, new[] { @"**\f*.dll", @"**\*.pdb" });

            // Assert
            Assert.AreEqual(2, files.Count());
            Assert.AreEqual(@"bin\debug\baz.dll", files[0].Path);
            Assert.AreEqual(@"bin\debug\notbaz.dll", files[1].Path);
        }

        private void AssertEqual(PathSearchFilter expected, PathSearchFilter actual) {
            Assert.AreEqual(expected.SearchDirectory, actual.SearchDirectory);
            Assert.AreEqual(expected.SearchOption, actual.SearchOption);
            Assert.AreEqual(expected.SearchPattern, actual.SearchPattern);
        }
    }
}
