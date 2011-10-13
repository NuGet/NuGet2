using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class DataServicePackageTest
    {
        [Fact]
        public void EmptyDependenciesStringReturnsEmptyDependenciesCollection()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "";

            // Assert
            Assert.False(((IPackage)servicePackage).Dependencies.Any());
        }

        [Fact]
        public void NullDependenciesStringReturnsEmptyDependenciesCollection()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Assert
            Assert.False(((IPackage)servicePackage).Dependencies.Any());
        }

        [Fact]
        public void DependenciesStringWithExtraSpaces()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "      A   :   1.3 | B :  [2.4, 5.0)   ";
            List<PackageDependency> dependencies = ((IPackage)servicePackage).Dependencies.ToList();

            // Assert
            Assert.Equal(2, dependencies.Count);
            Assert.Equal("A", dependencies[0].Id);
            Assert.True(dependencies[0].VersionSpec.IsMinInclusive);
            Assert.Equal(new SemanticVersion("1.3"), dependencies[0].VersionSpec.MinVersion);
            Assert.Equal("B", dependencies[1].Id);
            Assert.True(dependencies[1].VersionSpec.IsMinInclusive);
            Assert.Equal(new SemanticVersion("2.4"), dependencies[1].VersionSpec.MinVersion);
            Assert.False(dependencies[1].VersionSpec.IsMaxInclusive);
            Assert.Equal(new SemanticVersion("5.0"), dependencies[1].VersionSpec.MaxVersion);
        }

        [Fact]
        public void DownloadAndVerifyThrowsIfPackageHashIsNull()
        {
            // Arrange
            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2"
            };

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => servicePackage.DownloadAndVerifyPackage(new MockPackageRepository()), "Failed to download package correctly. The contents of the package could not be verified.");
        }

        [Fact]
        public void ShouldUpdateReturnsTrueIfOldHashAndPackageHashAreDifferent()
        {
            // Arrange
            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "NEWHASH"
            };

            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(new MockPackageRepository());

            // Assert
            Assert.True(shouldUpdate);
        }

        [Fact]
        public void ShouldUpdateReturnsTrueIfPackageNotInRepository()
        {
            // Arrange
            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "HASH",
                OldHash = "HASH"
            };

            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(new MockPackageRepository());

            // Assert
            Assert.True(shouldUpdate);
        }

        [Fact]
        public void ShouldUpdateReturnsTrueIfRepositoryPackageHashIsDifferentFromPackageHash()
        {
            // Arrange
            var servicePackage = new DataServicePackage
            {
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
            Assert.True(shouldUpdate);
        }

        [Fact]
        public void ShouldUpdateReturnsTrueIfRepositoryThrows()
        {
            // Arrange
            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "HASH",
                OldHash = "HASH"
            };

            var repository = new Mock<MockPackageRepository>();
            repository.Setup(m => m.GetPackages()).Throws(new Exception());

            // Act
            bool shouldUpdate = servicePackage.ShouldUpdatePackage(repository.Object);

            // Assert
            Assert.True(shouldUpdate);
        }
    }
}
