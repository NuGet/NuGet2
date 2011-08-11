using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class HttpClientTest {
        [TestMethod]
        public void CreateRequestHasDefaultProxyFinder() {
            // Arrange
            var httpClient = new HttpClient(new Uri("http://example.com"));

            // Act

            // Assert
            Assert.IsNotNull(httpClient.ProxyFinder, "HttpClient.ProxyFinder is not initialized by default.");
            Assert.AreEqual(HttpClient.DefaultProxyFinder, httpClient.ProxyFinder);
        }

        [TestMethod]
        public void ChangingDefaultProxyFinderShouldSetNewInstanceOnNewHttpClients() {
            // Arrange
            var mockProxyFinder = new Mock<IProxyFinder>();
            HttpClient.DefaultProxyFinder = mockProxyFinder.Object;
            var httpClient = new HttpClient(new Uri("http://example.com"), mockProxyFinder.Object, requestCredentialService: null);

            // Act

            // Assert
            Assert.AreEqual(mockProxyFinder.Object, httpClient.ProxyFinder);
        }

        [TestMethod]
        public void CreateRequestProxyFinderReturnsValidProxyForUri() {
            // Arrange            
            var proxyFinderMock = new Mock<IProxyFinder>();
            var validProxy = new WebProxy("http://someproxy");
            proxyFinderMock.Setup(finder => finder.GetProxy(It.IsAny<Uri>())).Returns(validProxy);
            var httpClient = new HttpClient(new Uri("http://example.com"), proxyFinderMock.Object, requestCredentialService: null);

            // Act
            var request = httpClient.CreateRequest();

            // Assert
            Assert.AreEqual(request.Proxy, validProxy);
        }

        [TestMethod]
        public void EmptyProviderListReturnsDefaultProxy() {
            // Arrange
            var httpClient = new HttpClient(new Uri("http://example.com"), proxyFinder: null, requestCredentialService: null);
            var defaultProxy = WebRequest.DefaultWebProxy;

            // Act
            var request = httpClient.CreateRequest();

            // Assert
            Assert.AreEqual(request.Proxy, defaultProxy);
        }

    }
}
