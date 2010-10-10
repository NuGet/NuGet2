using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Server.Controllers;
using NuPack.Server.Infrastructure;

namespace NuPack.Test.Server.Controllers {
    [TestClass]
    public class PackagesControllerTest {
        [TestMethod]
        public void DownloadWithFileThatHasNotBeenModifiedSinceLastRequestReturns304StatusResult() { 
            // Arrange
            var dateLastModified = DateTime.UtcNow.ToUniversalTime();
            var context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.Response.Cache.SetCacheability(HttpCacheability.Public));
            context.Setup(c => c.HttpContext.Response.Cache.SetLastModified(dateLastModified));
            var headers = new NameValueCollection() { { "If-Modified-Since", dateLastModified.ToString() } };
            context.Setup(c => c.HttpContext.Request.Headers).Returns(headers);
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.GetLastModified(It.IsAny<string>())).Returns(dateLastModified);
            var controller = new PackagesController(fileSystem.Object);
            controller.ControllerContext = context.Object;

            // Act
            var result = controller.Download("test.package.nupkg") as HttpStatusCodeResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(304, result.StatusCode);
        }
    }
}
