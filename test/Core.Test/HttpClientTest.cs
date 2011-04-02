using System;
using System.Net;
using System.Net.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class HttpClientTest {
        [TestMethod]
        public void CreateRequestUsesDefaultCredentials() {
            // Arrange
            var httpClient = new HttpClient();

            // Act
            WebRequest request = httpClient.CreateRequest(new Uri("http://example.com/"), acceptCompression: false);

            // Assert
            Assert.IsTrue(request.UseDefaultCredentials);
        }

        [TestMethod]
        public void InitializeRequestSetsProxyIfNull() {
            // Arrange
            var proxy = new Mock<IWebProxy>();
            proxy.SetupAllProperties();
            var request = new Mock<WebRequest>();
            request.Setup(r => r.Proxy).Returns(proxy.Object);
            var httpClient = new HttpClient();

            // Act
            httpClient.InitializeRequest(request.Object, acceptCompression: false);

            // Assert
            Assert.AreEqual(CredentialCache.DefaultCredentials, request.Object.Proxy.Credentials);
        }
    }
}
