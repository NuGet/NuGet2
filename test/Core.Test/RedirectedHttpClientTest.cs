using System;
using System.Net;
using Moq;
using Xunit;

namespace NuGet.Test
{
    public class RedirectedHttpClientTest
    {
        [Fact]
        public void CachedClientCachesResultsInMemoryCache()
        {
            // Arrange
            var uri = new Uri(@"http://nuget.org");
            var webResponse = new Mock<WebResponse>(MockBehavior.Strict);
            webResponse.Setup(s => s.ResponseUri).Returns(uri)
                                                 .Verifiable();
            var client = new Mock<HttpClient>(MockBehavior.Strict, uri);
            client.Setup(s => s.GetResponse()).Returns(webResponse.Object);
            var memoryCache = new MemoryCache();

            var redirectedClient1 = new Mock<RedirectedHttpClient>(uri, memoryCache) { CallBase = true };
            redirectedClient1.Setup(r => r.EnsureClient()).Returns(client.Object).Verifiable();

            var redirectedClient2 = new Mock<RedirectedHttpClient>(uri, memoryCache) { CallBase = true };

            
            // Act
            var result1 = redirectedClient1.Object.CachedClient;
            var result2 = redirectedClient2.Object.CachedClient;

            // Assert
            Assert.Same(result1, result2);
            redirectedClient1.Verify(r => r.EnsureClient(), Times.Once());
            redirectedClient2.Verify(r => r.EnsureClient(), Times.Never());
        }

        [Fact]
        public void ClientIsNotCachedIfExceptionOccursWhen()
        {
            // Arrange
            var uri = new Uri(@"http://nuget.org");
            var webResponse = new Mock<WebResponse>(MockBehavior.Strict);
            webResponse.Setup(s => s.ResponseUri).Returns(uri)
                                                 .Verifiable();
            var client = new Mock<HttpClient>(MockBehavior.Strict, uri);
            client.Setup(s => s.GetResponse()).Returns(webResponse.Object);
            var memoryCache = new MemoryCache();

            var redirectedClient1 = new Mock<RedirectedHttpClient>(uri, memoryCache) { CallBase = true };
            redirectedClient1.Setup(r => r.EnsureClient()).Throws(new Exception("Na na na na na")).Verifiable();

            var redirectedClient2 = new Mock<RedirectedHttpClient>(uri, memoryCache) { CallBase = true };
            redirectedClient2.Setup(r => r.EnsureClient()).Returns(client.Object);

            // Act and Assert
            ExceptionAssert.Throws<Exception>(() => redirectedClient1.Object.CachedClient.ToString(), "Na na na na na");
            var result2 = redirectedClient2.Object.CachedClient;

            // Assert
            Assert.Same(client.Object, result2);
            redirectedClient1.Verify(r => r.EnsureClient(), Times.Once());
            redirectedClient2.Verify(r => r.EnsureClient(), Times.Once());
        }
    }
}
