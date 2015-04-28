using Moq;
using Xunit;
using Xunit.Extensions;

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

        [Theory]
        [InlineData(@"C:\packages\")]
        [InlineData(@"\\folder\sub-folder")]
        [InlineData("file://some-folder/some-dir")]
        public void CreateRepositoryReturnsLocalRepositoryIfSourceIsPhysicalPath(string path)
        {
            // Arrange
            var factory = new PackageRepositoryFactory();

            // Act
            var repository = factory.CreateRepository(path);

            // Assert
            Assert.IsType<LazyLocalPackageRepository>(repository);
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
