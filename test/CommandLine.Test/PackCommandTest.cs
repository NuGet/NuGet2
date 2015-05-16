using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using Xunit;

namespace NuGet.Test.NuGetCommandLine.Commands
{
    public class PackCommandTest
    {
        [Fact]
        public void PackCommandDefaultFiltersRemovesManifestAndPackageFiles()
        {
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
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"x:\packagefiles\some-file\1.txt");
            Assert.Equal(files[1].Path, @"x:\packagefiles\folder\should-not-exclude\test.nupkg.html");
            Assert.Equal(files[2].Path, @"x:\packagefiles\test.nuspec.bkp");
        }

        [Fact]
        public void PackCommandDefaultFiltersRemovesRepoFiles()
        {
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
            Assert.Equal(2, files.Count);
            Assert.Equal(files[0].Path, @"x:\packagefiles\some-file\1.txt");
            Assert.Equal(files[1].Path, @"x:\packagefiles\folder\should-not-exclude\hg");
        }

        [Fact]
        public void PackCommandDefaultFiltersRemovesNugetFiles()
        {
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
            Assert.Equal(1, files.Count);
            Assert.Equal(files[0].Path, @"x:\packagefiles\some-file\1.txt");
        }

        [Fact]
        public void ExcludeFilesUsesWildCardExtension()
        {
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
            Assert.Equal(2, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.Equal(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [Fact]
        public void ExcludeFilesExcludesWildCardPaths()
        {
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
            Assert.Equal(2, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.Equal(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [Fact]
        public void ExcludeFilesDoesNotExcludeDefaultFilesIfExcludeSpecialPathsIsDisabled()
        {
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
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.Equal(files[1].Path, @"p:\some-file\should-not-be-removed\.ext\sample2.txt");
            Assert.Equal(files[2].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [Fact]
        public void ExcludeFilesPerformsCaseInsensitiveSearch()
        {
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
            Assert.Equal(2, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\should-not-be-removed\ext\sample.txt");
            Assert.Equal(files[1].Path, @"p:\some-file\should-not-be-removed\test.ext\sample3.jpg");
        }

        [Fact]
        public void ExcludeFilesDoesNotUseDefaultExcludesIfDisabled()
        {
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
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\test.txt");
            Assert.Equal(files[1].Path, @"p:\some-file\should-not-be-removed\ext\sample.nupkg");
            Assert.Equal(files[2].Path, @"p:\some-file\should-not-be-removed\.hgignore");
        }

        [Fact]
        public void ExcludeFilesUsesPathIfFileIsNotPhysicalPackageFile()
        {
            // Arrange
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(c => c.Path).Returns(@"content\foo.txt");
            var files = GetPackageFiles(@"p:\some-file\test.txt").Concat(new[] { mockFile.Object }).ToList();

            // Act
            var packCommand = new PackCommand { BasePath = @"p:\some-file", NoDefaultExcludes = true };
            packCommand.Exclude.Add(@"content\f*");
            packCommand.ExcludeFiles(files);

            // Assert
            Assert.Equal(1, files.Count);
            Assert.Equal(files[0].Path, @"p:\some-file\test.txt");
        }

        [Fact]
        public void GetInputFileThrowsIfNoFiles()
        {
            ExceptionAssert.Throws<CommandLineException>(() => PackCommand.GetInputFile(Enumerable.Empty<string>()), "Please specify a nuspec or project file to use.");
        }

        [Fact]
        public void GetInputFileMultipleProjectFilesThrows()
        {
            ExceptionAssert.Throws<CommandLineException>(() => PackCommand.GetInputFile(new[] { "a.csproj", "b.fsproj" }), "Please specify a nuspec or project file to use.");
        }

        [Fact]
        public void GetInputFileMultipleNuSpecFilesThrows()
        {
            ExceptionAssert.Throws<CommandLineException>(() => PackCommand.GetInputFile(new[] { "a.nuspec", "b.NuspeC" }), "Please specify a nuspec or project file to use.");
        }

        [Fact]
        public void GetInputFileNuSpecAndProjectFilePrefersProjectFile()
        {
            // Act
            string file = PackCommand.GetInputFile(new[] { "a.nuspec", "foo.csproj" });

            // Assert
            Assert.Equal("foo.csproj", file);
        }
        [Fact]
        public void ExcludeFilesForLibPackageRemovesAllPDBs()
        {
            // Arrange
            var files = GetPackageFiles(
                    @"lib\mylib.dll",
                    @"lib\mylib.pdb",
                    @"content\default.aspx",
                    @"content\extra.pdb",
                    @"tools\mycmd.exe",
                    @"tools\mycmd.pdb"
            );

            // Act
            PackCommand.ExcludeFilesForLibPackage(files);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"lib\mylib.dll");
            Assert.Equal(files[1].Path, @"content\default.aspx");
            Assert.Equal(files[2].Path, @"tools\mycmd.exe");
        }

        [Fact]
        public void ExcludeFilesForLibPackageRemovesAllFilesFromSrcTargetFolder()
        {
            // Arrange
            var files = GetPackageFiles(
                    @"lib\mylib.dll",
                    @"content\default.aspx",
                    @"content\default.aspx.cs",
                    @"src\foo.cs",
                    @"src\extra\nested\bar.cs",
                    @"src\extra\nested\bar.dll"
            );

            // Act
            PackCommand.ExcludeFilesForLibPackage(files);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"lib\mylib.dll");
            Assert.Equal(files[1].Path, @"content\default.aspx");
            Assert.Equal(files[2].Path, @"content\default.aspx.cs");
        }

        [Fact]
        public void ExcludeFilesForSymbolPackageRemovesAllContentFiles()
        {
            // Arrange
            var files = GetPackageFiles(
                    @"lib\mylib.dll",
                    @"lib\mylib.pdb",
                    @"content\default.aspx",
                    @"content\bin\extra.pdb",
                    @"src\mylib.cs"
            );

            // Act
            PackCommand.ExcludeFilesForSymbolPackage(files);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(files[0].Path, @"lib\mylib.dll");
            Assert.Equal(files[1].Path, @"lib\mylib.pdb");
            Assert.Equal(files[2].Path, @"src\mylib.cs");
        }

        [Fact]
        public void ExcludeFilesForSymbolPackageRemovesScripts()
        {
            // Arrange
            var files = GetPackageFiles(
                    @"lib\mylib.dll",
                    @"lib\mylib.pdb",
                    @"tools\mycmd.exe",
                    @"tools\init.ps1",
                    @"tools\extra.ps1",
                    @"src\mylib.cs"
            );

            // Act
            PackCommand.ExcludeFilesForSymbolPackage(files);

            // Assert
            Assert.Equal(4, files.Count);
            Assert.Equal(files[0].Path, @"lib\mylib.dll");
            Assert.Equal(files[1].Path, @"lib\mylib.pdb");
            Assert.Equal(files[2].Path, @"tools\mycmd.exe");
            Assert.Equal(files[3].Path, @"src\mylib.cs");
        }

        [Fact]
        public void PackCommandWarnsIfVersionContainsSpecialVersionButDoesNotConformToSemVer()
        {
            // Arrange
            var package = PackageUtility.CreatePackage("A", "1.0-alpha");
            var builder = new StringBuilder();
            var console = new Mock<IConsole>();

            console.Setup(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>())).Callback<string, object[]>((text, p) => builder.AppendFormat(text, p));
            console.Setup(c => c.WriteWarning(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback<bool, string, object[]>((b, text, p) => builder.AppendFormat(text, p));
            var packCommand = new PackCommand
            {
                Console = console.Object,
                Rules = Enumerable.Empty<IPackageRule>()
            };

            // Act
            packCommand.AnalyzePackage(package);

            // Assert
            Assert.Equal(@"1 issue(s) found with package 'A'.Issue: Use semantic versioningDescription: Version ""1.0-alpha"" does not follow semantic versioning guidelines.Solution: Update your nuspec file or use the AssemblyInformationalVersion assembly attribute to specify a semantic version as described at http://semver.org. ",
                builder.ToString());
        }

        private static IList<IPackageFile> GetPackageFiles(params string[] paths)
        {
            return (from p in paths
                    select new PhysicalPackageFile(useManagedCodeConventions: false) { SourcePath = p, TargetPath = p } as IPackageFile
            ).ToList();
        }
    }
}
