using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
            ExceptionAssert.Throws<NotSupportedException>(() => PackageRepositoryFactory.Default.CreateRepository(new PackageSource("a", "b") { IsAggregate = true }));
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
    }
}
