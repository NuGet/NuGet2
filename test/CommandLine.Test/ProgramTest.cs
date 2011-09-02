using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class ProgramTest {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void RemoveOldFileDeletesNuGetFileWithExtensionOldIfExist() {
            // Arrange
            var oldFilePath = Path.Combine(TestContext.TestDeploymentDir, "NuGet.exe.old");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(oldFilePath);

            // Act
            Program.RemoveOldFile(fileSystem);

            // Assert
            Assert.AreEqual(oldFilePath, fileSystem.Deleted.SingleOrDefault());
        }

        [TestMethod]
        public void RemoveOldFileDoesNotDeletesOldNuGetFileIfItDoesNotExistUnderWorkingDirectory() {
            // Arrange
            var oldFilePath = Path.Combine(TestContext.TestDeploymentDir, "foo", "NuGet.exe.old");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(oldFilePath);

            // Act
            Program.RemoveOldFile(fileSystem);

            // Assert
            Assert.IsFalse(fileSystem.Deleted.Any());
        }

        [TestMethod]
        public void RemoveOldDoesNotThrow() {
            // Arrange
            var oldFilePath = Path.Combine(TestContext.TestDeploymentDir, "foo", "NuGet.exe.old");
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystem.Setup(c => c.FileExists(oldFilePath)).Returns(true);
            fileSystem.Setup(c => c.DeleteFile(oldFilePath)).Throws(new Exception("Can't touch this."));

            // Act
            Program.RemoveOldFile(fileSystem.Object);

            // Assert
            // If we've got this far, we have not thrown.
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetCommandLineSettingsReturnsSettingsFromLocalFileIfExists() {
            // Arrange
            var fileContent = @"<?xml version=""1.0""?><configuration><fooSection><add key=""barValue"" value=""qux"" /></fooSection></configuration>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("NuGet.config", fileContent.AsStream());

            // Act
            var settings = Program.GetCommandLineSettings(fileSystem);
            var value = settings.GetValue("fooSection", "barValue");

            // Assert
            Assert.AreEqual("qux", value);
        }
    }
}
