using System.Linq;
using Moq;
using Xunit;

namespace NuGet.Test
{

    public class PackageRepositoryFactoryTest
    {
        [Fact]
        public void CreateRepositoryThrowsIfNullSource()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new PackageRepositoryFactory().CreateRepository(null), "packageSource");
        }

        [Fact]
        public void CreateRepositoryReturnsLocalRepositoryIfSourceIsPhysicalPath()
        {
            // Arrange
            var paths = new[] { @"C:\packages\", 
                                 @"\\folder\sub-folder",
                                 "file://some-folder/some-dir"};
            var factory = new PackageRepositoryFactory();

            // Act and Assert
            Assert.True(paths.Select(factory.CreateRepository)
                               .All(p => p is LocalPackageRepository));
        }

        [Fact]
        public void CreateRepositoryReturnsDataServicePackageRepositoryIfSourceIsWebUrl()
        {
            // Arrange
            var httpClient = new Mock<IHttpClient>();
            httpClient.SetupAllProperties();
            var factory = new PackageRepositoryFactory();

            // Act
            IPackageRepository repository = factory.CreateRepository("http://example.com/");

            // Act and Assert
            Assert.IsType(typeof(DataServicePackageRepository), repository);
        }
    }
}
