using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuPack.Test.Mocks;

namespace NuPack.Test {
    [TestClass]
    public class PackageSyndicationFeedTest {
        [TestMethod]
        public void InvalidPackageIsExcludedFromFeedItems() {
            // Arrange
            var repository = new MockPackageRepository();
            IPackage goodPackage = PackageUtility.CreatePackage("A", "1.0");
            var badPackage = new Mock<IPackage>();
            badPackage.Setup(m => m.Id).Returns("B");
            badPackage.Setup(m => m.Version).Returns(new Version("1.0"));            
            repository.AddPackage(badPackage.Object);
            repository.AddPackage(goodPackage);

            // Act
            var feed = PackageSyndicationFeed.Create(repository, p => new Uri("http://package/" + p.Id));

            // Assert
            Assert.AreEqual(1, feed.Items.Count());
            var item = feed.Items.Single();
            Assert.AreEqual("A", item.Title.Text);
        }
    }
}
