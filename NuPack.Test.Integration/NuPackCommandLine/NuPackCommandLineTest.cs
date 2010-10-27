using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.Integration.NuGetCommandLine {
    [TestClass]
    public class NuPackCommandLineTest {
        private const string NoSpecsfolder = @".\nospecs\";
        private const string OneSpecfolder = @".\onespec\";
        private const string TwoSpecsFolder = @".\twospecs\";
        private const string OutputFolder = @".\output\";
        private const string SpecificFilesFolder = @".\specific_files\";
        private const string NupackExePath = @".\NuGet.exe";

        [TestInitialize]
        public void Initialize() {
            DeleteDirs();

            Directory.CreateDirectory(NoSpecsfolder);
            Directory.CreateDirectory(OneSpecfolder);
            Directory.CreateDirectory(TwoSpecsFolder);
            Directory.CreateDirectory(SpecificFilesFolder);
            Directory.CreateDirectory(OutputFolder);
        }

        [TestCleanup]
        public void Cleanup() {
            DeleteDirs();
        }

        [TestMethod]
        public void NuPackCommandLine_ShowsHelpIfThereIsNoCommand() {
            // Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, NoSpecsfolder, string.Empty, true);

            // Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("usage: NuGet <command> [args] [options]"));
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereAreNoNuSpecFiles() {
            // Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, NoSpecsfolder, "pack", true);
            // Assert
            Assert.AreEqual(1, result.Item1);
            Assert.AreEqual("Please specify a nuspec file to use.", result.Item2.Trim());
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereIsMoreThanOneNuSpecFile() {
            // Arrange
            string nuspecFile = Path.Combine(TwoSpecsFolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            string nuspecFile2 = Path.Combine(TwoSpecsFolder, "antlr2.nuspec");
            File.WriteAllText(nuspecFile2, NuSpecFileContext.FileContents);

            // Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, TwoSpecsFolder, "pack", true);

            // Assert
            Assert.AreEqual(1, result.Item1);
            Assert.AreEqual("Please specify a nuspec file to use.", result.Item2.Trim());
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingNoArgsAndThereOneNuSpecFile() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);

            //Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, OneSpecfolder, "pack", true);

            //Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("Successfully created package"));
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingBasePath() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "Antlr.nuspec");
            string expectedPackage = Path.Combine(OutputFolder, "Antlr.3.1.1.nupkg");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);

            //Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, OneSpecfolder, "pack -o " + @"..\output\", true);

            //Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));
        }

        [TestMethod]
        public void PackageCommand_SpecifyingFilesInNuspecOnlyPackagesSpecifiedFiles() {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = Path.Combine(SpecificFilesFolder, "test.1.1.1.nupkg");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file1.txt"), "file 1");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file2.txt"), "file 2");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file3.txt"), "file 3");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
  </metadata>
  <files>
    <file src=""file1.txt"" target=""content"" />
  </files>
</package>");

            // Act
            Tuple<int, string> result = CommandRunner.Run(NupackExePath, SpecificFilesFolder, "pack", true);

            // Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt" });
        }

        private void VerifyPackageContents(string packageFile, IEnumerable<string> expectedFiles) {
            var package = new ZipPackage(packageFile);
            var files = package.GetFiles().Select(f => f.Path).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(expectedFiles.OrderBy(f => f).ToList(), files);
        }

        private static void DeleteDirs() {
            DeleteDir(NoSpecsfolder);
            DeleteDir(OneSpecfolder);
            DeleteDir(TwoSpecsFolder);
            DeleteDir(SpecificFilesFolder);
            DeleteDir(OutputFolder);
        }

        private static void DeleteDir(string directory) {
            try {
                if (Directory.Exists(directory)) {
                    foreach (var file in Directory.GetFiles(directory)) {
                        try {
                            File.Delete(file);
                        }
                        catch (FileNotFoundException) {

                        }
                    }
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (DirectoryNotFoundException) {

            }
        }
    }
}