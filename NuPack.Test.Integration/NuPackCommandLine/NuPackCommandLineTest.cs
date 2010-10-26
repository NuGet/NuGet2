using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test.Integration.NuPackCommandLine {
    [TestClass]
    public class NuPackCommandLineTest {

        private const string nupackExePath = @".\NuPack.exe";

        [TestMethod]
        public void NuPackCommandLine_ShowsHelpIfThereIsNoCommand() {
            // Arrange
            const string folder = @".\nospecs\";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            // Act
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, folder, string.Empty, true);
            // Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("usage: NuPack <command> [args] [options]"));
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereAreNoNuSpecFiles() {
            // Arrange
            const string folder = @".\nospecs\";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            // Act
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, folder, "pack", true);
            // Assert
            Assert.AreEqual(1, result.Item1);
            Assert.AreEqual("Please specify a nuspec file to use.", result.Item2.Trim());
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereIsMoreThanOneNuSpecFile() {
            // Arrange
            const string folder = @".\twospecs\";
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            string nuspecFile = Path.Combine(folder, "antlr.nuspec");
            if (File.Exists(nuspecFile)) {
                File.Delete(nuspecFile);
            }
            File.AppendAllText(nuspecFile, NuSpecFileContext.FileContents);

            string nuspecFile2 = Path.Combine(folder, "antlr2.nuspec");
            if (File.Exists(nuspecFile2)) {
                File.Delete(nuspecFile2);
            }
            File.AppendAllText(nuspecFile2, NuSpecFileContext.FileContents);

            // Act
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, folder, "pack", true);

            // Assert
            Assert.AreEqual(1, result.Item1);
            Assert.AreEqual("Please specify a nuspec file to use.", result.Item2.Trim());
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingNoArgsAndThereOneNuSpecFile() {
            //Arrange
            const string folder = @".\onespec\";

            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            string nuspecFile = Path.Combine(folder, "antlr.nuspec");
            if (File.Exists(nuspecFile)) {
                File.Delete(nuspecFile);
            }

            File.AppendAllText(nuspecFile, NuSpecFileContext.FileContents);

            //Act
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, folder, "pack", true);

            //Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("Successfully created package"));
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingBasePath() {
            //Arrange
            if (!Directory.Exists(@".\onespec\")) {
                Directory.CreateDirectory(@".\onespec\");
            }
            if (!Directory.Exists(@".\output\")) {
                Directory.CreateDirectory(@".\output\");
            }

            string nuspecFile = Path.Combine(@".\onespec\", "Antlr.nuspec");
            string expectedPackage = @".\output\Antlr.3.1.1.nupkg";

            if (File.Exists(nuspecFile)) {
                File.Delete(nuspecFile);
            }

            if (File.Exists(expectedPackage)) {
                File.Delete(expectedPackage);
            }

            File.AppendAllText(nuspecFile, NuSpecFileContext.FileContents);

            //Act
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, @".\onespec\", "pack -o " + @"..\output\", true);

            //Assert
            Assert.AreEqual(0, result.Item1);
            Assert.IsTrue(result.Item2.Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));
        }

        [TestMethod]
        public void PackageCommand_SpecifyingFilesInNuspecOnlyPackagesSpecifiedFiles() {
            // Arrange
            const string folder = @".\specific_files\";
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            string nuspecFile = Path.Combine(folder, "SpecWithFiles.nuspec");
            string expectedPackage = Path.Combine(folder, "test.1.1.1.nupkg");
            if (File.Exists(nuspecFile)) {
                File.Delete(nuspecFile);
            }

            if (File.Exists(expectedPackage)) {
                File.Delete(expectedPackage);
            }

            File.WriteAllText(Path.Combine(folder, "file1.txt"), "file 1");
            File.WriteAllText(Path.Combine(folder, "file2.txt"), "file 2");
            File.WriteAllText(Path.Combine(folder, "file3.txt"), "file 3");
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
            Tuple<int, string> result = CommandRunner.Run(nupackExePath, folder, "pack", true);

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
    }
}