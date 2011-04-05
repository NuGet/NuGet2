using System.Linq;
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
    }
}
