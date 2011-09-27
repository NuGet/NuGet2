using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.VisualStudio.Test {
    using PackageUtility = NuGet.Test.PackageUtility;

    public class RecentPackageTest {
        [Fact]
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
            Assert.Equal("A", package.Id);
            Assert.Equal(new SemanticVersion("1.2"), package.Version);
            Assert.Equal(2, contents.Count);
            Assert.Equal(1.0, package.Rating);
            Assert.Equal("content\\one", content1);
            Assert.Equal("content\\two", content2);
        }

        private string ReadStream(Stream stream) {
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }
}
