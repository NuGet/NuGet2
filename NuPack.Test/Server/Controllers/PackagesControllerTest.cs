using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Server.Controllers;
using NuPack.Server.Infrastructure;

namespace NuPack.Test.Server.Controllers {
    [TestClass]
    public class PackagesControllerTest {
        // Disabled tests to fix perf issue with server
        //[TestMethod]
        public void DownloadReturnsConditionalGetResultWithLastModifiedFromPackageFile() {
            // Arrange
            var dateLastModified = DateTimeOffset.Now;
            var repository = new Mock<IPackageStore>();
            repository.Setup(f => f.GetLastModified(It.IsAny<string>())).Returns(dateLastModified);
            var controller = new PackagesController(repository.Object);
            var context = new Mock<ControllerContext>();
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("nupack.nupkg") as ConditionalGetResult;

            // Assert
            Assert.AreEqual(dateLastModified, result.LastModified);
        }

        //[TestMethod]
        public void DownloadReturnsConditionalGetResultWithCorrectFileResult() {
            // Arrange
            var dateLastModified = DateTime.UtcNow;
            var repository = new Mock<IPackageStore>();
            repository.Setup(f => f.GetFullPath("nupack.nupkg")).Returns(@"c:\packages\nupack.nupkg");
            var controller = new PackagesController(repository.Object);
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
