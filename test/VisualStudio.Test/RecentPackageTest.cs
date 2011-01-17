using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.VisualStudio.Test {
    
    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class RecentPackageTest {

        [TestMethod]
        public void TestTheFirstConstructor() {
            // Arrange
            IPackage basePackage = PackageUtility.CreatePackage(
                "A",
                "1.2",
                content: new string[] { "one", "two" },
                rating: 1.0);

            // Act
            var package = new RecentPackage(basePackage, "http://bing.com");
            var contents = package.GetContentFiles().ToList();
            var content1 = ReadStream(contents[0].GetStream());
            var content2 = ReadStream(contents[1].GetStream());

            // Assert
            Assert.AreEqual("A", package.Id);
            Assert.AreEqual(new Version("1.2"), package.Version);
            Assert.AreEqual(2, contents.Count);
            Assert.AreEqual(1.0, package.Rating);
            Assert.AreEqual("content\\one", content1);
            Assert.AreEqual("content\\two", content2);
            Assert.AreEqual("http://bing.com", package.Source);
        }

        [TestMethod]
        public void TestTheSecondConstructor() {
            // Arrange
            IPackage basePackage = PackageUtility.CreatePackage(
                "B",
                "2.1",
                content: new string[] { "three", "four" },
                rating: 2.0);

            var mockMetadata = new Mock<IPersistencePackageMetadata>();
            mockMetadata.Setup(p => p.Id).Returns("B");
            mockMetadata.Setup(p => p.Version).Returns(new Version("2.1"));
            mockMetadata.Setup(p => p.Source).Returns("http://live.com");

            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p=>p.GetPackages()).Returns((new IPackage[] { basePackage}).AsQueryable());

            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            mockRepositoryFactory.Setup(p => p.CreateRepository(It.Is<PackageSource>(source => source.Source == "http://live.com"))).Returns(mockRepository.Object);

            // Act
            var package = new RecentPackage(mockMetadata.Object, mockRepositoryFactory.Object);
            var contents = package.GetContentFiles().ToList();
            var content1 = ReadStream(contents[0].GetStream());
            var content2 = ReadStream(contents[1].GetStream());

            // Assert
            Assert.AreEqual("B", package.Id);
            Assert.AreEqual(new Version("2.1"), package.Version);
            Assert.AreEqual(2, contents.Count);
            Assert.AreEqual(2.0, package.Rating);
            Assert.AreEqual("content\\three", content1);
            Assert.AreEqual("content\\four", content2);
            Assert.AreEqual("http://live.com", package.Source);
        }

        private string ReadStream(Stream stream) {
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }
}
