using System;
using System.Linq;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class MachineCacheTest {
        [TestMethod]
        public void AddingMoreThanPackageLimitClearsCache() {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            MachineCache cache = new MachineCache(mockFileSystem);
            for (int i = 0; i < 100; i++) {
                cache.AddPackage(PackageUtility.CreatePackage("A", i + ".0"));
            }

            // Act
            cache.AddPackage(PackageUtility.CreatePackage("B"));

            // Assert
            Assert.AreEqual(1, cache.GetPackageFiles().Count());
        }

        [TestMethod]
        public void ClearRemovesAllPackageFilesFromCache() {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            MachineCache cache = new MachineCache(mockFileSystem);
            for (int i = 0; i < 20; i++) {
                cache.AddPackage(PackageUtility.CreatePackage("A", i + ".0"));
            }

            // Act
            cache.Clear();

            // Assert
            Assert.IsFalse(cache.GetPackageFiles().Any());
        }

        [TestMethod]
        public void MachineCacheUsesNullFileSystemIfItCannotAccessCachePath() {
            // Arrange
            Func<string> getCachePathDirectory = () => { throw new SecurityException("Boo"); };
            var package = PackageUtility.CreatePackage("TestPackage");

            // Act
            MachineCache cache = MachineCache.CreateDefault(getCachePathDirectory);

            // Assert
            Assert.IsNotNull(cache);
            Assert.IsFalse(cache.GetPackageFiles().Any());

            // Ensure operations don't throw
            cache.Clear();
            cache.AddPackage(PackageUtility.CreatePackage("TestPackage"));
            Assert.IsFalse(cache.Exists("TestPackage"));
            Assert.IsFalse(cache.Exists(package));
            Assert.IsFalse(cache.Exists("TestPackage", new Version("1.0")));
            Assert.IsNull(cache.ResolveDependency(new PackageDependency("Bar")));
            Assert.IsNull(cache.FindPackage("TestPackage"));
            Assert.IsFalse(cache.FindPackages(new[] { "TestPackage", "B" }).Any());
            Assert.IsFalse(cache.FindPackagesById("TestPackage").Any());
            Assert.IsFalse(cache.GetPackages().Any());
            Assert.IsFalse(cache.GetUpdates(new[] { package }).Any());
            cache.RemovePackage(package);
            Assert.AreEqual(0, cache.Source.Length);
            Assert.IsFalse(cache.TryFindPackage("TestPackage", new Version("1.0"), out package));
        }
    }
}
