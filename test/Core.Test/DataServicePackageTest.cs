using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class DataServicePackageTest {
        [TestMethod]
        public void EmptyDependenciesStringReturnsEmptyDependenciesCollection() {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "";

            // Assert
            Assert.IsFalse(((IPackage)servicePackage).Dependencies.Any());
        }

        [TestMethod]
        public void NullDependenciesStringReturnsEmptyDependenciesCollection() {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Assert
            Assert.IsFalse(((IPackage)servicePackage).Dependencies.Any());
        }

        [TestMethod]
        public void DependenciesStringWithExtraSpaces() {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "      A   :   1.3 | B :  [2.4, 5.0)   ";
            List<PackageDependency> dependencies = ((IPackage)servicePackage).Dependencies.ToList();

            // Assert
            Assert.AreEqual(2, dependencies.Count);
            Assert.AreEqual("A", dependencies[0].Id);
            Assert.IsTrue(dependencies[0].VersionSpec.IsMinInclusive);
            Assert.AreEqual(new Version("1.3"), dependencies[0].VersionSpec.MinVersion);
            Assert.AreEqual("B", dependencies[1].Id);
            Assert.IsTrue(dependencies[1].VersionSpec.IsMinInclusive);
            Assert.AreEqual(new Version("2.4"), dependencies[1].VersionSpec.MinVersion);
            Assert.IsFalse(dependencies[1].VersionSpec.IsMaxInclusive);
            Assert.AreEqual(new Version("5.0"), dependencies[1].VersionSpec.MaxVersion);
        }

        [TestMethod]
        public void DownloadAndVerifyThrowsIfPackageHashIsNull() {
            // Arrange
            var servicePackage = new DataServicePackage {
                Id = "A",
                Version = "1.2"
            };

            // Act & Asert
            ExceptionAssert.Throws<InvalidOperationException>(() => servicePackage.DownloadAndVerifyPackage(new MockPackageRepository()), "Failed to download package correctly. The contents of the package could not be verified.");
        }

        [TestMethod]
        public void ShouldUpdateReturnsTrueIfOldHashAndPackageHashAreDifferent() {
            // Arrange
            var servicePackage = new DataServicePackage {
                Id = "A",
                Version = "1.2",
                PackageHash = "NEWHASH"
            };

            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(new MockPackageRepository());

            // Assert
            Assert.IsTrue(shouldUpdate);
        }

        [TestMethod]
        public void ShouldUpdateReturnsTrueIfPackageNotInRepository() {
            // Arrange
            var servicePackage = new DataServicePackage {
                Id = "A",
                Version = "1.2",
                PackageHash = "HASH",
                OldHash = "HASH"
            };

            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(new MockPackageRepository());

            // Assert
            Assert.IsTrue(shouldUpdate);
        }

        [TestMethod]
        public void ShouldUpdateReturnsTrueIfRepositoryPackageHashIsDifferentFromPackageHash() {
            // Arrange
            var servicePackage = new DataServicePackage {
                Id = "A",
                Version = "1.2",
                PackageHash = "HASH",
                OldHash = "HASH"
            };

            var repository = new MockPackageRepository {
                PackageUtility.CreatePackage("A", "1.2")
            };
            
            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(repository);

            // Assert
            Assert.IsTrue(shouldUpdate);
        }
    }
}
