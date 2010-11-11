using System;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageDownloaderTest {
        [TestMethod]
        public void CtorWithNullHttpClientThrowsArgumentNullException() {
            // Arrange, Act, Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => new PackageDownloader(null));
        }

        [TestMethod]
        public void CtorSetsUserAgent() {
            // Arrange
            var httpClient = new Mock<IHttpClient>();
            httpClient.SetupProperty(c => c.UserAgent);

            // Act
            var downloader = new PackageDownloader(httpClient.Object);

            // Assert
            Assert.IsTrue(httpClient.Object.UserAgent.StartsWith("Package-Installer/"));
        }

        [TestMethod]
        public void DownloadPackageWithUnverifiedPackageThrowsInvalidDataException() {
            // Arrange
            var response = new Mock<WebResponse>();
            response.Setup(r => r.GetResponseStream()).Returns(new MemoryStream(new byte[] { 123 }));
            var request = new Mock<WebRequest>();
            request.Setup(r => r.GetResponse()).Returns(response.Object);
            var httpClient = new Mock<IHttpClient>();
            httpClient.Setup(c => c.CreateRequest(It.IsAny<Uri>())).Returns(request.Object);
            var hashProvider = new Mock<IHashProvider>();
            hashProvider.Setup(h => h.VerifyHash(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(false);
            var packageFactory = new Mock<IPackageFactory>();
            packageFactory.Setup(f => f.CreatePackage(It.IsAny<Func<Stream>>())).Returns(new Mock<IPackage>().Object).Callback<Func<Stream>>(streamFactory => streamFactory());
            var downloader = new PackageDownloader(httpClient.Object, packageFactory.Object, hashProvider.Object);

            // Act, Assert
            ExceptionAssert.Throws<InvalidDataException>(() => downloader.DownloadPackage(new Uri("http://example.com/"), new byte[] { }, useCache: false));
        }

        [TestMethod]
        public void DownloadPackageReturnsCachedBytes() {
            // Arrange
            var response = new Mock<WebResponse>();
            response.Setup(r => r.GetResponseStream()).Returns(new MemoryStream(new byte[] { 78, 117, 71, 101, 116 }));
            var request = new Mock<WebRequest>();
            request.Setup(r => r.GetResponse()).Returns(response.Object);
            var httpClient = new Mock<IHttpClient>();
            int httpClientCallbackCount = 0;
            httpClient.Setup(c => c.CreateRequest(It.IsAny<Uri>())).Returns(request.Object).Callback(() => httpClientCallbackCount++);

            Func<Stream> streamFactory = null;
            var packageFactory = new Mock<IPackageFactory>();
            packageFactory.Setup(f => f.CreatePackage(It.IsAny<Func<Stream>>())).Returns(new Mock<IPackage>().Object).Callback<Func<Stream>>(sf => streamFactory = sf);
            var downloader = new PackageDownloader(httpClient.Object, packageFactory.Object, null);

            // Act
            downloader.DownloadPackage(new Uri("http://example.com"), new byte[] { }, useCache: true);

            // Assert
            var stream = streamFactory(); // HttpClient is invoked
            var cachedStream = streamFactory(); // HttpClient should not be invoked.
            Assert.AreEqual(1, httpClientCallbackCount);
            Assert.AreNotSame(stream, cachedStream);
            var streamContents = stream.ReadToEnd();
            Assert.AreEqual(streamContents, cachedStream.ReadToEnd());
            Assert.AreEqual("NuGet", streamContents);
        }
    }

}
