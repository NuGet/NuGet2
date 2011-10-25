using System;
using System.Linq;
using System.Security;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class MachineCacheTest
    {
        [Fact]
        public void AddingMoreThanPackageLimitClearsCache()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            MachineCache cache = new MachineCache(mockFileSystem);
            for (int i = 0; i < 100; i++)
            {
                cache.AddPackage(PackageUtility.CreatePackage("A", i + ".0"));
            }

            // Act
            cache.AddPackage(PackageUtility.CreatePackage("B"));

            // Assert
            Assert.Equal(1, cache.GetPackageFiles().Count());
        }

        [Fact]
        public void ClearRemovesAllPackageFilesFromCache()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            MachineCache cache = new MachineCache(mockFileSystem);
            for (int i = 0; i < 20; i++)
            {
                cache.AddPackage(PackageUtility.CreatePackage("A", i + ".0"));
            }

            // Act
            cache.Clear();

            // Assert
            Assert.False(cache.GetPackageFiles().Any());
        }

        [Fact]
        public void MachineCacheUsesNullFileSystemIfItCannotAccessCachePath()
        {
            // Arrange
            Func<string> getCachePathDirectory = () => { throw new SecurityException("Boo"); };
            var package = PackageUtility.CreatePackage("TestPackage");

            // Act
            MachineCache cache = MachineCache.CreateDefault(getCachePathDirectory);

            // Assert
            Assert.NotNull(cache);
            Assert.False(cache.GetPackageFiles().Any());

            // Ensure operations don't throw
            cache.Clear();
            cache.AddPackage(PackageUtility.CreatePackage("TestPackage"));
            Assert.False(cache.Exists("TestPackage"));
            Assert.False(cache.Exists(package));
            Assert.False(cache.Exists("TestPackage", new SemanticVersion("1.0")));
            Assert.Null(cache.ResolveDependency(new PackageDependency("Bar"), false));
            Assert.Null(cache.FindPackage("TestPackage"));
            Assert.False(cache.FindPackages(new[] { "TestPackage", "B" }).Any());
            Assert.False(cache.FindPackagesById("TestPackage").Any());
            Assert.False(cache.GetPackages().Any());
            Assert.False(cache.GetUpdates(new[] { package }, includePrerelease: true, includeAllVersions: false).Any());
            cache.RemovePackage(package);
            Assert.Equal(0, cache.Source.Length);
            Assert.False(cache.TryFindPackage("TestPackage", new SemanticVersion("1.0"), out package));
        }
    }
}
