using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Commands;

namespace NuGet.Test.NuGetCommandLine.Commands {
    [TestClass]
    public class PackCommandTest {
        [TestMethod]
        public void PackCommandDefaultFiltersRemovesRepoFiles() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\1.txt",
                    @"z:\folder\.hg",
                    @"z:\folder\should-not-exclude\hg",
                    @"x:\repo\.git\HEAD",
                    @"y:\svnrepo\.svn\all-wcrops"
            );

            // Act
            var packCommand = new PackCommand();
            PackCommand.ExcludeFiles(files, packCommand.Exclude);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\1.txt");
            Assert.AreEqual(files[1].Path, @"z:\folder\should-not-exclude\hg");
        }

        [TestMethod]
        public void PackCommandDefaultFiltersRemovesNugetFiles() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\1.txt",
                    @"x:\foo\bar.nupkg",
                    @"y:\bar\test.nuspec"
            );

            // Act
            var packCommand = new PackCommand();
            PackCommand.ExcludeFiles(files, packCommand.Exclude);

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\1.txt");
        }

        [TestMethod]
        public void ExcludeFilesUsesWildCardExtension() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\should-be-removed\test.ext",
                    @"p:\some-file\should-not-be-removed\ext\sample.txt",
                    @"p:\some-file\should-not-be-removed\.ext\sample2.txt",
                    @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg"
            );

            // Act
            PackCommand.ExcludeFiles(files, new[] { "*.ext" });

            // Assert
            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\.ext\sample2.txt");
            Assert.AreEqual(files[2].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [TestMethod]
        public void ExcludeFilesExcludesWildCardPaths() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\should-be-removed\test.ext",
                    @"p:\some-file\should-not-be-removed\ext\sample.txt",
                    @"p:\some-file\should-not-be-removed\.ext\sample2.txt",
                    @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg"
            );

            // Act
            PackCommand.ExcludeFiles(files, new[] { ".ext*" });

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
        }

        [TestMethod]
        public void ExcludeFilesPerformsCaseInsensitiveSearch() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\should-be-removed\test.ext",
                    @"p:\some-file\should-not-be-removed\ext\sample.txt",
                    @"p:\some-file\should-not-be-removed\.ext\sample2.txt",
                    @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg"
            );

            // Act
            PackCommand.ExcludeFiles(files, new[] { ".EXt*" });

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
        }

        private static IList<IPackageFile> GetPackageFiles(params string[] paths) {
            return (from p in paths
                    select new PhysicalPackageFile { SourcePath = p, TargetPath = p } as IPackageFile).ToList();
        }
    }
}
