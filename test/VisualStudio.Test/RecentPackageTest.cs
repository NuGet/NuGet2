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
            var package = basePackage;
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
        }

        private string ReadStream(Stream stream) {
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }
}
