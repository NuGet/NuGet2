using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ninject;
using Ninject.Activation;
using NuGet.Server.Infrastructure;

namespace NuGet.Test.Server.Infrastructure {
    [TestClass]
    public class NinjectControllerFactoryTest {
        [TestMethod]
        public void GetControllerInstanceRetrievesInstanceFromKernel() {
            // Arrange
            var controller = new TestController();
            var kernel = new Mock<IKernel>();
            kernel.Setup(k => k.Resolve(It.IsAny<IRequest>())).Returns(new[] { controller });
            var factory = new NinjectControllerFactory(kernel.Object);

            // Act
            var createdController = factory.CreateControllerInstance(null, typeof(TestController));

            // Assert
            Assert.AreEqual(controller, createdController);
        }

        [TestMethod]
        public void GetControllerInstanceFallsBackToDefaultBehaviorWhenResolvingControllerReturnsNull() {
            // Arrange
            var kernel = new Mock<IKernel>();
            kernel.Setup(k => k.Resolve(It.IsAny<IRequest>())).Returns(new Controller[] { });
            var factory = new NinjectControllerFactory(kernel.Object);

            // Act
            var controller = factory.CreateControllerInstance(null, typeof(TestController));

            // Assert
            Assert.IsInstanceOfType(controller, typeof(TestController));
        }

        [TestMethod]
        public void GetControllerInstanceFallsBackToDefaultBehaviorWhenControllerTypeIsNull() {
            // Arrange
            var kernel = new Mock<IKernel>();
            kernel.Setup(k => k.Resolve(It.IsAny<IRequest>())).Returns(new Controller[] { });
            var factory = new NinjectControllerFactory(kernel.Object);
            var httpRequest = new Mock<HttpRequestBase>();
            httpRequest.Setup(m => m.Path).Returns("FakePath");
            var httpContext = new Mock<HttpContextBase>();
            httpContext.Setup(m => m.Request).Returns(httpRequest.Object);
            var requestContext = new Mock<RequestContext>();
            requestContext.Setup(m => m.HttpContext).Returns(httpContext.Object);

            // Act
            ExceptionAssert.Throws<HttpException>(() => factory.CreateControllerInstance(requestContext.Object, null));
        }

        public class TestController : IController {
            public void Execute(System.Web.Routing.RequestContext requestContext) {
                throw new NotImplementedException();
            }
        }
    }
}
