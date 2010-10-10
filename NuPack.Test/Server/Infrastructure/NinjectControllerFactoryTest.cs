using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ninject;
using Ninject.Activation;
using NuPack.Server.Infrastructure;

namespace NuPack.Test.Server.Infrastructure {
    [TestClass]
    public class NinjectControllerFactoryTest {
        [TestMethod]
        public void GetControllerInstanceRetrievesInstanceFromKernel() {
            // Arrange
            var controller = new TestController();
            var kernel = new Mock<IKernel>();
            kernel.Setup(k => k.Resolve(It.IsAny<IRequest>())).Returns(new[]{controller});
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


        public class TestController : IController {
            public void Execute(System.Web.Routing.RequestContext requestContext) {
                throw new NotImplementedException();
            }
        }
    }
}
