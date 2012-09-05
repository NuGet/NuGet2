using System.Linq;
using Moq;
using NuGet.Test;
using Xunit;

namespace NuGet.Common.Test
{
    public class CachedPackageSourceProviderTest
    {
        [Fact]
        public void CachedPackageSourceProviderThrowsIfSourceProviderIsNull()
        {
            // Arrange
            IPackageSourceProvider sourceProvider = null;

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => new CachedPackageSourceProvider(sourceProvider), "sourceProvider");
        }

        [Fact]
        public void CachedPackageSourceProviderReadsSourcesFromInnerProvider()
        {
            // Arrange
            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceA = new PackageSource("SourceA");
            var sourceB = new PackageSource("SourceB", "Source B", isEnabled: false);

            sourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { sourceA, sourceB });
            
            // Act
            var cachedPackageSource = new CachedPackageSourceProvider(sourceProvider.Object);
            var result = cachedPackageSource.LoadPackageSources().ToList();

            // Assert
            Assert.Equal(new[] { sourceA, sourceB }, result);
        }

        [Fact]
        public void CachedPackageSourceProviderReadsEnabledSourceValues()
        {
            // Arrange
            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceA = new PackageSource("SourceA", "SourceA", isEnabled: false);
            var sourceB = new PackageSource("SourceB", "Source B");
            sourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { sourceA, sourceB });

            // Act
            var cachedPackageSource = new CachedPackageSourceProvider(sourceProvider.Object);
            var result = cachedPackageSource.GetEnabledPackageSources().ToList();

            // Assert
            Assert.Equal(new[] { sourceB }, result);
        }

        [Fact]
        public void CachedPackageSourceProviderReadsCredentials()
        {
            // Arrange
            var sourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceA = new PackageSource("SourceA") { UserName = "Username", Password = "password" };
            var sourceB = new PackageSource("SourceB", "Source B", isEnabled: false);

            sourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { sourceA, sourceB });

            // Act
            var cachedPackageSource = new CachedPackageSourceProvider(sourceProvider.Object);
            var result = cachedPackageSource.GetEnabledPackageSources().First();

            // Assert
            Assert.Equal("Username", result.UserName);
            Assert.Equal("password", result.Password);
        }
    }
}
