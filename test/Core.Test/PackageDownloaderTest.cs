using System;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageDownloaderTest {
        [TestMethod]
        public void DownloadPackageWithUnverifiedPackageThrowsInvalidDataException() {
            // Arrange
            var downloadClient = new Mock<IHttpClient>();
            downloadClient.Setup(c => c.DownloadData()).Returns(new byte[] { 123 });
            var hashProvider = new Mock<IHashProvider>();
            hashProvider.Setup(h => h.VerifyHash(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(false);
            var packageFactory = new Mock<IPackageFactory>();
            packageFactory.Setup(f => f.CreatePackage(It.IsAny<Func<Stream>>())).Returns(new Mock<IPackage>().Object).Callback<Func<Stream>>(streamFactory => streamFactory());
            var downloader = new PackageDownloader(packageFactory.Object, hashProvider.Object);

            var package = PackageUtility.CreatePackage("A", "1.0");

            // Act, Assert
            ExceptionAssert.Throws<InvalidDataException>(() => downloader.DownloadPackage(downloadClient.Object, new byte[0], package));
        }
    }
}
