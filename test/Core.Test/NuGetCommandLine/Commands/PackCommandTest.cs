using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;

namespace NuGet.Test.NuGetCommandLine.Commands {
    [TestClass]
    public class PackCommandTest {
        [TestMethod]
        public void PackCommandDefaultFiltersRemovesManifestAndPackageFiles() {
            // Arrange
            var files = GetPackageFiles(
                    @"x:\packagefiles\some-file\1.txt",
                    @"x:\packagefiles\folder\test.nupkg",
                    @"x:\packagefiles\folder\should-not-exclude\test.nupkg.html",
                    @"x:\packagefiles\test.nuspec",
                    @"x:\packagefiles\test.nuspec.bkp",
                    @"x:\packagefiles\subdir\foo.nuspec"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"x:\packagefiles\", NoDefaultExcludes = false };
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(files[0].Path, @"x:\packagefiles\some-file\1.txt");
            Assert.AreEqual(files[1].Path, @"x:\packagefiles\folder\should-not-exclude\test.nupkg.html");
            Assert.AreEqual(files[2].Path, @"x:\packagefiles\test.nuspec.bkp");
        }

        [TestMethod]
        public void PackCommandDefaultFiltersRemovesRepoFiles() {
            // Arrange
            var files = GetPackageFiles(
                    @"x:\packagefiles\some-file\1.txt",
                    @"x:\packagefiles\folder\.hg",
                    @"x:\packagefiles\folder\should-not-exclude\hg",
                    @"x:\packagefiles\repo\.git\HEAD",
                    @"x:\packagefiles\svnrepo\.svn\all-wcrops",
                    @"x:\packagefiles\.git\should-not-exist"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"x:\packagefiles\", NoDefaultExcludes = false };
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(files[0].Path, @"x:\packagefiles\some-file\1.txt");
            Assert.AreEqual(files[1].Path, @"x:\packagefiles\folder\should-not-exclude\hg");
        }

        [TestMethod]
        public void PackCommandDefaultFiltersRemovesNugetFiles() {
            // Arrange
            var files = GetPackageFiles(
                    @"x:\packagefiles\some-file\1.txt",
                    @"x:\packagefiles\foo\bar.nupkg",
                    @"x:\packagefiles\bar\test.nuspec"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"x:\packagefiles", NoDefaultExcludes = false };
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(files[0].Path, @"x:\packagefiles\some-file\1.txt");
        }

        [TestMethod]
        public void ExcludeFilesUsesWildCardExtension() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\should-be-removed\test.ext",
                    @"p:\some-file\should-not-be-removed\ext\sample.txt",
                    @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = false };
            packCommand.Exclude.Add(@"**\*.ext");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
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
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = false };
            packCommand.Exclude.Add(@"**\*.ext");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [TestMethod]
        public void ExcludeFilesDoesNotExcludeDefaultFilesIfExcludeSpecialPathsIsDisabled() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\should-be-removed\test.ext",
                    @"p:\some-file\should-not-be-removed\ext\sample.txt",
                    @"p:\some-file\should-not-be-removed\.ext\sample2.txt",
                    @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = true };
            packCommand.Exclude.Add(@"**\*.ext");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\.ext\sample2.txt");
            Assert.AreEqual(files[2].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
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
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = false };
            packCommand.Exclude.Add(@"**\*.EXt");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [TestMethod]
        public void ExcludeFilesDoesNotUseDefaultExcludesIfDisabled() {
            // Arrange
            var files = GetPackageFiles(
                    @"p:\some-file\test.txt",
                    @"p:\some-file\should-not-be-removed\ext\sample.nupkg",
                    @"p:\some-file\manifest.nuspec",
                    @"p:\some-file\should-not-be-removed\.hgignore",
                    @"p:\some-file\should-be-removed\file.ext"
            );

            // Act
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = true };
            packCommand.Exclude.Add(@"**\*.ext");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\test.txt");
            Assert.AreEqual(files[1].Path, @"p:\some-file\should-not-be-removed\ext\sample.nupkg");
            Assert.AreEqual(files[2].Path, @"p:\some-file\should-not-be-removed\.hgignore");
        }

        [TestMethod]
        public void ExcludeFilesUsesPathIfFileIsNotPhysicalPackageFile() {
            // Arrange
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(c => c.Path).Returns(@"content\foo.txt");
            var files = GetPackageFiles(@"p:\some-file\test.txt").Concat(new[] { mockFile.Object }).ToList();

            // Act
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = true };
            packCommand.Exclude.Add(@"content\f*");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(files[0].Path, @"p:\some-file\test.txt");
        }

        private static IList<IPackageFile> GetPackageFiles(params string[] paths) {
            return (from p in paths
                    select new PhysicalPackageFile { SourcePath = p, TargetPath = p } as IPackageFile).ToList();
        }
    }
}
