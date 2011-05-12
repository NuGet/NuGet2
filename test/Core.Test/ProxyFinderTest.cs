using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class ProxyFinderTest {
        [TestMethod]
        public void RegisteringMoreThanOneInstanceOfSameProviderOnlyKeepsOne() {
            // Arrange
            var proxyFinder = new ProxyFinder();
            var mockProvider = new Mock<IProxyProvider>();

            // Act
            Enumerable.Repeat(mockProvider.Object, 2).ToList().ForEach(proxyFinder.RegisterProvider);

            // Assert
            Assert.IsTrue(proxyFinder.RegisteredProviders.Count == 1);
        }

        [TestMethod]
        public void UnregisteringNonRegisteredProvidersShouldNotThrowExceptions() {
            // Arrange
            var proxyFinder = new ProxyFinder();
            var mockProvider = new Mock<IProxyProvider>();

            // Act
            proxyFinder.UnregisterProvider(mockProvider.Object);

            // Assert
            Assert.IsTrue(proxyFinder.RegisteredProviders.Count == 0);
        }
    }
}
