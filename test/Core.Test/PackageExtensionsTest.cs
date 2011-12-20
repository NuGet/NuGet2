using System.Linq;
using Xunit;

namespace NuGet.Test
{
    public class PackageExtensionsTest
    {
        [Fact]
        public void FindPackagesOverloadLooksForSearchTermsInSpecificFields()
        {
            // Arrange
            var packages = new[] {
                PackageUtility.CreatePackage("Foo.Qux", description: "Some desc"),
                PackageUtility.CreatePackage("X-Package", tags: " lib qux "),
                PackageUtility.CreatePackage("Filtered"),
                PackageUtility.CreatePackage("B", description: "This is a package for qux and not one for baz"),
            };

            // Act
            var result1 = packages.AsQueryable().Find(new[] { "Description", "Tags" }, "Qux");
            var result2 = packages.AsQueryable().Find(new[] { "Id" }, "Filtered");

            // Assert
            Assert.Equal(new[] { packages[1], packages[3] }, result1.ToArray());
            Assert.Equal(new[] { packages[2], }, result2.ToArray());
        }

        [Fact]
        public void FindPackagesOverloadReturnsEmptySequenceIfTermIsNotFoundInProperties()
        {
            // Arrange
            var packages = new[] {
                PackageUtility.CreatePackage("Foo.Qux"),
                PackageUtility.CreatePackage("X-Package", tags: " lib qux "),
                PackageUtility.CreatePackage("Filtered"),
                PackageUtility.CreatePackage("B", description: "This is a package for qux and not one for baz"),
            };

            // Act
            var result1 = packages.AsQueryable().Find(new[] { "Summary" }, "Qux");

            // Assert
            Assert.Empty(result1);
        }
    }
}
