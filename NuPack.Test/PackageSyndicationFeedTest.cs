using System;
using System.Linq;
using System.ServiceModel.Syndication;
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

        [TestMethod]
        public void CreatingFeedConvertsPackagesToAtomEntries() {
            // Arrange
            var repository = new MockPackageRepository();
            IPackage package = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(package);

            // Act
            var feed = PackageSyndicationFeed.Create(repository, p => new Uri("http://package/" + p.Id));

            // Assert
            Assert.AreEqual(1, feed.Items.Count());
            var item = feed.Items.Single();
            Assert.AreEqual("A", item.Title.Text);
            Assert.AreEqual("1.0", item.ElementExtensions.ReadElementExtensions<string>("version", Constants.SchemaNamespace).Single());
            Assert.AreEqual("en-US", item.ElementExtensions.ReadElementExtensions<string>("language", Constants.SchemaNamespace).Single());
            Assert.AreEqual("Tester", item.Authors[0].Name);
            Assert.AreEqual("enclosure", item.Links[0].RelationshipType);
            Assert.AreEqual("license", item.Links[1].RelationshipType);
            Assert.AreEqual(new Uri("ftp://test/somelicense.txts"), item.Links[1].Uri);
            Assert.AreEqual("Mock package A", ((TextSyndicationContent)item.Content).Text);          
        }
    }
}
