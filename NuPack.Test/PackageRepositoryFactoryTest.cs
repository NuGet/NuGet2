using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PackageRepositoryFactoryTest {
        [TestMethod]
        public void CreateRepositoryThrowsIfNullOrEmpty() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => PackageRepositoryFactory.Default.CreateRepository(null), "source");
        }

        [TestMethod]
        public void CreateRepositoryReturnsLocalRepositoryIfSourceIsPhysicalPath() {
            // Arrange
            var paths = new[] { @"C:\packages\", 
                                 @"\\folder\sub-folder",
                                 "file://some-folder/some-dir"};
            var factory = PackageRepositoryFactory.Default;

            // Act and Assert
            Assert.IsTrue(paths.Select(factory.CreateRepository)
                               .All(p => p is LocalPackageRepository));
        }
    }
}
