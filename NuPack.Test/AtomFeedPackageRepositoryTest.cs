using System;
using System.IO;
using System.Net;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuPack.Test {
    [TestClass]
    public class AtomFeedPackageRepositoryTest {
        [TestMethod]
        public void AtomFeedConstructedithNullUriThrowsException() {
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new AtomFeedPackageRepository(null), "feedUri");
        }

        [TestMethod]
        public void AtomFeedThrowsWhenLoadingFromInvalidFeedSource() {
            // Arrange
            var feedUri = new Uri("http://some-website-that-hopefully-should-not-exist/foo-bar");
            var repo = new AtomFeedPackageRepository(feedUri);
            Func<Stream> getStream = () => { throw new WebException(); };

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => AtomFeedPackageRepository.GetFeedItems(getStream), "Unable to read feed. Verify that a feed is hosted at the remote server and is available.");
        }

        [TestMethod]
        public void AtomFeedThrowsWhenReadingInvalidContent() {
            // Arrange
            var xmlReader = new XmlTextReader(new StringReader("malformed xml"));

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => AtomFeedPackageRepository.GetFeedItems(xmlReader),
                "Unable to read feed contents. Verify that the feed conforms to the Atom Syndication format.");
        }
    }
}