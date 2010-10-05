namespace NuPack.Test.Integration.NuPackCommandLine {

    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NuPackCommandLineTest {

        private const string nupackExePath = @".\NuPack.exe";

        [TestMethod]
        public void WhenNotPassingArgumentsAndThereAreNoNuSpecFilesItShouldError() {
            const string folder = @".\nospecs\";

            //Arrange
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            //create no files

            //Act
            string result = CommandRunner.Run(nupackExePath, folder, string.Empty, true);

            //Assert
            Assert.AreEqual("Usage: NuPack.exe <manifest-file>", result.Trim());
        }

        [TestMethod]
        public void WhenNotPassingArgumentsThereAreTwoNuSpecFilesItShouldError() {
            const string folder = @".\twospecs\";

            //Arrange
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

            //Act
            string result = CommandRunner.Run(nupackExePath, folder, string.Empty, true);

            //Assert
            Assert.AreEqual("Specify which nuspec file to use because this folder has more than one.", result.Trim());
        }

        [TestMethod]
        public void WhenNotPassingArgumentsAndThereIsOneNuSpecFileItShouldExecuteTheFile() {
            const string folder = @".\onespec\";

            //Arrange
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string nuspecFile = Path.Combine(folder, "antlr.nuspec");
            if (File.Exists(nuspecFile)) File.Delete(nuspecFile);
            File.AppendAllText(nuspecFile, NuSpecFileContext.FileContents);

            //Act
            string result = CommandRunner.Run(nupackExePath, folder, string.Empty, true);

            //Assert
            Assert.AreNotEqual("Usage: NuPack.exe <manifest-file>", result.Trim());
            Assert.AreNotEqual("Specify which nuspec file to use because this folder has more than one.", result.Trim());
            Assert.AreEqual(true, result.Contains("successfully"));
        }
    }
}