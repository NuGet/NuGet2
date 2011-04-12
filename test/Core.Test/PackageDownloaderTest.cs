using System;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageDownloaderTest {
        [TestMethod]
        public void CtorSetsUserAgent() {
            // Arrange
            var httpClient = new Mock<IHttpClient>();
            httpClient.SetupProperty(c => c.UserAgent);

            // Act
            var downloader = new PackageDownloader(httpClient.Object);

            // Assert
            Assert.IsTrue(httpClient.Object.UserAgent.StartsWith("NuGet Visual Studio Extension/"));
        }

        [TestMethod]
        public void DownloadPackageWithUnverifiedPackageThrowsInvalidDataException() {
            // Arrange
            var response = new Mock<WebResponse>();
            response.Setup(r => r.GetResponseStream()).Returns(new MemoryStream(new byte[] { 123 }));
            var request = new Mock<WebRequest>();
            request.Setup(r => r.GetResponse()).Returns(response.Object);
            var httpClient = new Mock<IHttpClient>();
            httpClient.Setup(c => c.CreateRequest(It.IsAny<Uri>(), It.IsAny<bool>())).Returns(request.Object);
            var hashProvider = new Mock<IHashProvider>();
            hashProvider.Setup(h => h.VerifyHash(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(false);
            var packageFactory = new Mock<IPackageFactory>();
            packageFactory.Setup(f => f.CreatePackage(It.IsAny<Func<Stream>>())).Returns(new Mock<IPackage>().Object).Callback<Func<Stream>>(streamFactory => streamFactory());
            var downloader = new PackageDownloader(httpClient.Object, packageFactory.Object, hashProvider.Object);

            var package = PackageUtility.CreatePackage("A", "1.0");

            // Act, Assert
            ExceptionAssert.Throws<InvalidDataException>(() => downloader.DownloadPackage(new Uri("http://example.com/"), new byte[0], package));
        }
    }
}
