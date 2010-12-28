using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Server.Infrastructure;

namespace NuGet.Test.Server.Infrastructure {
    [TestClass]
    public class ConditionalGetResultTest {
        [TestMethod]
        public void GetActionResultReturnsHttpStatusCodeResultWith302StatusWhenRequestUnmodified() {
            // Arrange
            var dateLastModified = DateTime.UtcNow;
            var headers = new NameValueCollection() { { "If-Modified-Since", dateLastModified.ToString("r") } };
            var context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.Request.Headers).Returns(headers);
            var conditionalResult = new ConditionalGetResult(dateLastModified, null);

            // Act
            var result = conditionalResult.GetActionResult(context.Object) as HttpStatusCodeResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(304, result.StatusCode);
        }

        [TestMethod]
        public void GetActionResultReturnsActionResultWhenFileModifiedAfterHeader() {
            // Arrange
            var dateLastModified = DateTime.UtcNow;
            var headers = new NameValueCollection() { { "If-Modified-Since", dateLastModified.AddMinutes(-10).ToString("r") } };
            var context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.Request.Headers).Returns(headers);
            var conditionalResult = new ConditionalGetResult(dateLastModified, () => new EmptyResult());

            // Act
            var result = conditionalResult.GetActionResult(context.Object) as EmptyResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SetCacheSetCachePolicySetsCacheabilityToPublic() {
            // Arrange
            var context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.Response.Cache.SetCacheability(HttpCacheability.Public)).Verifiable();
            var conditionalResult = new ConditionalGetResult(DateTime.Now, () => new EmptyResult());

            // Act
            conditionalResult.SetCachePolicy(context.Object);

            // Assert
            context.VerifyAll();
        }

        [TestMethod]
        public void SetCacheSetCachePolicySetsLastModifiedCorrectly() {
            // Arrange
            var dateLastModified = DateTime.UtcNow;
            var context = new Mock<ControllerContext>();
            context.Setup(c => c.HttpContext.Response.Cache.SetLastModified(dateLastModified)).Verifiable();
            var conditionalResult = new ConditionalGetResult(dateLastModified, () => new EmptyResult());

            // Act
            conditionalResult.SetCachePolicy(context.Object);

            // Assert
            context.VerifyAll();
        }
    }
}
