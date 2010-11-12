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
            ExceptionAssert.ThrowsArgNull(() => PackageRepositoryFactory.Default.CreateRepository(null), "packageSource");
        }

        [TestMethod]
        public void CreateRepositoryThrowsIfAggregateRepository() {
            // Act & Assert
            ExceptionAssert.Throws<NotSupportedException>(() => PackageRepositoryFactory.Default.CreateRepository(new PackageSource("b", "a") { IsAggregate = true }));
        }

        [TestMethod]
        public void CreateRepositoryReturnsLocalRepositoryIfSourceIsPhysicalPath() {
            // Arrange
            var paths = new[] { @"C:\packages\", 
                                 @"\\folder\sub-folder",
                                 "file://some-folder/some-dir"};
            var factory = PackageRepositoryFactory.Default;

            // Act and Assert
            Assert.IsTrue(paths.Select(p => factory.CreateRepository(new PackageSource(p, p)))
                               .All(p => p is LocalPackageRepository));
        }

        [TestMethod]
        public void CreateRepositoryReturnsDataServicePackageRepositoryIfSourceIsWebUrl() {
            // Arrange
            var httpClient = new Mock<IHttpClient>();
            httpClient.SetupAllProperties();
            httpClient.Setup(c => c.GetRedirectedUri(It.IsAny<Uri>())).Returns(new Uri("http://example.com"));
            var factory = new PackageRepositoryFactory(httpClient.Object);

            // Act
            IPackageRepository repository = factory.CreateRepository(new PackageSource("http://example.com/", "Test Source"));

            // Act and Assert
            Assert.IsInstanceOfType(repository, typeof(DataServicePackageRepository));
        }
    }
}
