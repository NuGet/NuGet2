namespace NuPack.Test.Integration.NuPackCommandLine {

    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Assert.AreEqual(true, result.Item2.Contains("usage: NuPack <command> [args] [options]"));
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
            Assert.AreEqual(true, result.Item2.Contains("Successfully created package"));
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
            Assert.AreEqual(true, result.Item2.Contains("Successfully created package"));
            Assert.AreEqual(true, File.Exists(expectedPackage));

        }
    }
}