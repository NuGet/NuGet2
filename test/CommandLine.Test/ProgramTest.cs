using System;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class ProgramTest
    {
        [Fact]
        public void RemoveOldFileDeletesNuGetFileWithExtensionOldIfExist()
        {
            // Arrange
            var oldFilePath = GetOldExePath();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(oldFilePath);

            // Act
            Program.RemoveOldFile(fileSystem);

            // Assert
            Assert.Equal(oldFilePath, fileSystem.Deleted.SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void RemoveOldFileDoesNotDeletesOldNuGetFileIfItDoesNotExistUnderWorkingDirectory()
        {
            // Arrange
            var oldFilePath = GetOldExePathUnderSubdirectory();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(oldFilePath);

            // Act
            Program.RemoveOldFile(fileSystem);

            // Assert
            Assert.False(fileSystem.Deleted.Any());
        }

        [Fact]
        public void RemoveOldDoesNotThrow()
        {
            // Arrange
            var oldFilePath = GetOldExePathUnderSubdirectory();
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystem.Setup(c => c.FileExists(oldFilePath)).Returns(true);
            fileSystem.Setup(c => c.DeleteFile(oldFilePath)).Throws(new Exception("Can't touch this."));

            // Act
            Program.RemoveOldFile(fileSystem.Object);

            // Assert
            // If we've got this far, we have not thrown.
            Assert.True(true);
        }

        [Fact]
        public void GetCommandLineSettingsReturnsSettingsFromLocalFileIfExists()
        {
            // Arrange
            var fileContent = @"<?xml version=""1.0""?><configuration><fooSection><add key=""barValue"" value=""qux"" /></fooSection></configuration>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("NuGet.config", fileContent.AsStream());

            // Act
            var settings = Program.GetCommandLineSettings(fileSystem);
            var value = settings.GetValue("fooSection", "barValue");

            // Assert
            Assert.Equal("qux", value);
        }

        private static string GetOldExePath()
        {
            var path = typeof(NuGet.Program).Assembly.Location;
            return Path.Combine(Path.GetDirectoryName(path), "NuGet.exe.old");
        }

        private static string GetOldExePathUnderSubdirectory()
        {
            var path = typeof(NuGet.Program).Assembly.Location;
            return Path.Combine(Path.GetDirectoryName(path), "sub-directory", "NuGet.exe.old");
        }
    }
}
