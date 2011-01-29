using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace NuGet.Test.Integration.NuGetCommandLine {
    [TestClass]
    public class NuGetCommandLineTest {
        private const string NoSpecsfolder = @".\nospecs\";
        private const string OneSpecfolder = @".\onespec\";
        private const string TwoSpecsFolder = @".\twospecs\";
        private const string OutputFolder = @".\output\";
        private const string SpecificFilesFolder = @".\specific_files\";
        private const string NugetExePath = @".\NuGet.exe";

        private StringWriter consoleOutput;
        private TextWriter originalConsoleOutput;
        private TextWriter originalErrorConsoleOutput;


        [TestInitialize]
        public void Initialize() {
            DeleteDirs();

            Directory.CreateDirectory(NoSpecsfolder);
            Directory.CreateDirectory(OneSpecfolder);
            Directory.CreateDirectory(TwoSpecsFolder);
            Directory.CreateDirectory(SpecificFilesFolder);
            Directory.CreateDirectory(OutputFolder);

            originalConsoleOutput = System.Console.Out;
            originalErrorConsoleOutput = System.Console.Error;
            consoleOutput = new StringWriter();
            System.Console.SetOut(consoleOutput);
            System.Console.SetError(consoleOutput);
        }

        [TestCleanup]
        public void Cleanup() {
            DeleteDirs();
            System.Console.SetOut(originalConsoleOutput);
            System.Console.SetError(originalErrorConsoleOutput);
        }


        [TestMethod]
        public void NuGetCommandLine_ShowsHelpIfThereIsNoCommand() {
            // Arrange 
            string[] args = new string[0];
            
            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("usage: NuGet <command> [args] [options]"));
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereAreNoNuSpecFiles() {
            // Arrange 
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(NoSpecsfolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.AreEqual("Please specify a nuspec file to use.", consoleOutput.ToString().Trim());
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereIsMoreThanOneNuSpecFile() {
            // Arrange
            string nuspecFile = Path.Combine(TwoSpecsFolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            string nuspecFile2 = Path.Combine(TwoSpecsFolder, "antlr2.nuspec");
            File.WriteAllText(nuspecFile2, NuSpecFileContext.FileContents);
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(TwoSpecsFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.AreEqual("Please specify a nuspec file to use.", consoleOutput.ToString().Trim());
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingNoArgsAndThereOneNuSpecFile() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingBasePath() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "Antlr.nuspec");
            string expectedPackage = Path.Combine("..\\output\\", "Antlr.3.1.1.nupkg");
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            string[] args = new string[] { "pack", "-o", "..\\output\\" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));
        }

        [TestMethod]
        public void PackageCommand_SpecifyingFilesInNuspecOnlyPackagesSpecifiedFiles() {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = "test.1.1.1.nupkg";
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
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt" });
        }

        [TestMethod]
        public void PackageCommand_WhenErrorIsThrownPackageFileIsDeleted() {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithErrors.nuspec");
            string expectedPackage = "hello world.1.1.1.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>hello world</id>
    <version>1.1.1</version>
    <authors>Bar</authors>
    <description>Foo</description>
    <language>en-US</language>
  </metadata>
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.IsFalse(File.Exists(expectedPackage));
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