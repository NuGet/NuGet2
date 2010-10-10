using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Server.Controllers;
using NuPack.Server.Infrastructure;

namespace NuPack.Test.Server.Controllers {
    [TestClass]
    public class PackagesControllerTest {
        [TestMethod]
        public void DownloadReturnsConditionalGetResultWithLastModifiedFromPackageFile() {
            // Arrange
            var dateLastModified = DateTime.UtcNow.ToUniversalTime();
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.GetLastModified(It.IsAny<string>())).Returns(dateLastModified);
            var controller = new PackagesController(fileSystem.Object);
            var context = new Mock<ControllerContext>();
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("nupack.nupkg") as ConditionalGetResult;

            // Assert
            Assert.AreEqual(dateLastModified, result.LastModified);
        }

        [TestMethod]
        public void DownloadReturnsConditionalGetResultWithCorrectFileResult() {
            // Arrange
            var dateLastModified = DateTime.UtcNow.ToUniversalTime();
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.Root).Returns(@"c:\packages");
            var controller = new PackagesController(fileSystem.Object);
            var context = new Mock<ControllerContext>();
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("nupack.nupkg") as ConditionalGetResult;

            // Assert
            var actualResult = result.ConditionalActionResult() as FileResult;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(@"nupack.nupkg", actualResult.FileDownloadName);
            Assert.AreEqual(@"application/zip", actualResult.ContentType);
        }
    }
}
