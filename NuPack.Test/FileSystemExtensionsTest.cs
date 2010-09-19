namespace NuPack.Test {
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuPack.Test.Mocks;
    using Moq;

    [TestClass]
    public class FileSystemExtensionsTest {
        [TestMethod]
        public void AddFilesAddFilesToProjectSystem() {
            // Arrange
            var fileSystem = new MockProjectSystem();
            var files = PackageUtility.CreateFiles(new[] { "A", "B", "C" });

            // Act
            fileSystem.AddFiles(files, NullLogger.Instance);

            // Assert
            Assert.IsTrue(fileSystem.FileExists("A"));
            Assert.IsTrue(fileSystem.FileExists("B"));
            Assert.IsTrue(fileSystem.FileExists("C"));
        }

        [TestMethod]
        public void AddFilesAddFilesToProjectSystemIfNotExists() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.AddFile(It.IsAny<string>(), It.IsAny<Stream>())).Verifiable();
            mockFileSystem.Setup(m => m.FileExists("A")).Returns(true);
            var files = PackageUtility.CreateFiles(new[] { "A", "B", "C" });

            // Act
            mockFileSystem.Object.AddFiles(files, NullLogger.Instance);

            // Assert
            mockFileSystem.Verify(m => m.AddFile("A", It.IsAny<Stream>()), Times.Never());
        }
    }
}
