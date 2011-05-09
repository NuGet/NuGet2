using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test {
    [TestClass]
    public class PackageRepositoryFactoryTest {
        [TestMethod]
        public void CreateRepositoryThrowsIfNullSource() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new PackageRepositoryFactory().CreateRepository(null), "packageSource");
        }

        [TestMethod]
        public void CreateRepositoryReturnsLocalRepositoryIfSourceIsPhysicalPath() {
            // Arrange
            var paths = new[] { @"C:\packages\", 
                                 @"\\folder\sub-folder",
                                 "file://some-folder/some-dir"};
            var factory = new PackageRepositoryFactory();

            // Act and Assert
            Assert.IsTrue(paths.Select(factory.CreateRepository)
                               .All(p => p is LocalPackageRepository));
        }

        [TestMethod]
        public void CreateRepositoryReturnsDataServicePackageRepositoryIfSourceIsWebUrl() {
            // Arrange
            var httpClient = new Mock<IHttpClient>();
            httpClient.SetupAllProperties();
            var factory = new PackageRepositoryFactory();

            // Act
            IPackageRepository repository = factory.CreateRepository("http://example.com/");

            // Act and Assert
            Assert.IsInstanceOfType(repository, typeof(DataServicePackageRepository));
        }
    }
}
