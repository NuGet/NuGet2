using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class PackageUtilityTest
    {
        [Fact]
        public void IsSatellitePackageReturnsFalseForNullLanguage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo");

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenMissingLanguageSuffix()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo", language: "fr-fr");

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageHandlesBadPackageId()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage(".fr-fr", language: "fr-fr");

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenMissingDependency()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr");

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageReturnsFalseWhenRuntimePackageNotInRepository()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr", dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0]")) });

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, targetFramework: null, runtimePackage: out runtimePackage);

            // Assert
            Assert.False(isSatellite);
            Assert.Null(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageReturnsTrueWhenRuntimePackageIdentified()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var runtime = PackageUtility.CreatePackage("foo");
            var package = PackageUtility.CreatePackage("foo.fr-fr", language: "fr-fr", dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0]")) });

            repository.AddPackage(runtime);

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.True(isSatellite);
            Assert.NotNull(runtimePackage);
        }

        [Fact]
        public void IsSatellitePackageIgnoresCaseOnLanguage()
        {
            // Arrange
            var repository = new MockPackageRepository();
            var runtime = PackageUtility.CreatePackage("foo");
            var package = PackageUtility.CreatePackage("foo.Fr-Fr", language: "fr-FR", dependencies: new[] { new PackageDependency("foo", VersionUtility.ParseVersionSpec("[1.0]")) });

            repository.AddPackage(runtime);

            // Act
            IPackage runtimePackage;
            var isSatellite = NuGet.PackageHelper.IsSatellitePackage(package, repository, null, out runtimePackage);

            // Assert
            Assert.True(isSatellite);
            Assert.NotNull(runtimePackage);
        }
    }
}
