using System;
using System.Net;
using System.Net.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace NuGet.Test {
    [TestClass]
    public class HttpClientTest {
        [TestMethod]
        public void CreateRequestHasDefaultProxyFinder() {
            // Arrange
            var httpClient = new HttpClient(new Uri("http://example.com"));

            // Act
            // Assert
            Assert.IsNotNull(httpClient.ProxyFinder,"HttpClient.ProxyFinder is not initialized by default.");
        }

        [TestMethod]
        public void CreateRequestProxyFinderReturnsValidProxyForUri() {
            // Arrange
            var httpClient = new HttpClient(new Uri("http://example.com"));
            var proxyFinderMock = new Mock<IProxyFinder>();
            var validProxy = new WebProxy("http://someproxy");
            proxyFinderMock.Setup(finder => finder.GetProxy(It.IsAny<Uri>())).Returns(validProxy);
            httpClient.ProxyFinder = proxyFinderMock.Object;

            // Act
            var request = httpClient.CreateRequest();
            
            // Assert
            Assert.AreEqual(request.Proxy,validProxy);
        }

        [TestMethod]
        public void EmptyStrategyListReturnsNullProxy() {
            // Arrange
            var httpClient = new HttpClient(new Uri("http://example.com"));
            
            // Act
            httpClient.ProxyFinder = null;
            var request = httpClient.CreateRequest();

            // Assert
            Assert.IsNull(request.Proxy);
        }

    }
}
