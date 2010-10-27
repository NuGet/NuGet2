using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Server.Controllers;
using NuGet.Server.Infrastructure;

namespace NuGet.Test.Server.Controllers {
    [TestClass]
    public class PackagesControllerTest {        
        [TestMethod]
        public void DownloadReturnsConditionalGetResultWithLastModifiedFromPackageFile() {
            // Arrange
            var dateLastModified = DateTimeOffset.Now;
            var store = new Mock<IPackageStore>();
            var repository = new Mock<IPackageRepository>();
            store.Setup(f => f.GetLastModified(It.IsAny<string>())).Returns(dateLastModified);
            var controller = new PackagesController(store.Object, repository.Object);
            var context = new Mock<ControllerContext>();
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("NuGet.nupkg") as ConditionalGetResult;

            // Assert
            Assert.AreEqual(dateLastModified, result.LastModified);
        }

        [TestMethod]
        public void DownloadReturnsConditionalGetResultWithCorrectFileResult() {
            // Arrange
            var dateLastModified = DateTime.UtcNow;
            var store = new Mock<IPackageStore>();
            var repository = new Mock<IPackageRepository>();
            store.Setup(f => f.GetFullPath("NuGet.nupkg")).Returns(@"c:\packages\NuGet.nupkg");
            var controller = new PackagesController(store.Object, repository.Object);
            var context = new Mock<ControllerContext>();
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("NuGet.nupkg") as ConditionalGetResult;

            // Assert
            var actualResult = result.ConditionalActionResult() as FileResult;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(@"NuGet.nupkg", actualResult.FileDownloadName);
            Assert.AreEqual(@"application/zip", actualResult.ContentType);
        }
    }
}
