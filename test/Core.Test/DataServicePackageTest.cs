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
        public void EnsurePackageDownloadsThePackageIfItIsNotCachedInMemoryOnInMachineCache()
        {
            // Arrange
            var zipPackage = PackageUtility.CreatePackage("A", "1.2");
            var uri = new Uri("http://nuget.org");
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>()))
                             .Returns(zipPackage)
                             .Verifiable();
            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            var mockRepository = new MockPackageRepository();
            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "NEWHASH",
                Downloader = packageDownloader.Object,
                HashProvider = hashProvider.Object,
                Context = context.Object
            };

            // Act
            servicePackage.EnsurePackage(mockRepository);

            // Assert
            context.Verify();
            packageDownloader.Verify();
            Assert.True(mockRepository.Exists(zipPackage));
        }

        [Fact]
        public void EnsurePackageDownloadsUsesInMemoryCachedInstanceOnceDownloaded()
        {
            // Arrange
            var zipPackage = PackageUtility.CreatePackage("A", "1.2");
            var uri = new Uri("http://nuget.org");
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>()))
                             .Returns(zipPackage)
                             .Verifiable();
            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            var mockRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            mockRepository.Setup(s => s.AddPackage(zipPackage)).Verifiable();
            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "NEWHASH",
                Downloader = packageDownloader.Object,
                HashProvider = hashProvider.Object,
                Context = context.Object
            };

            // Act
            servicePackage.EnsurePackage(mockRepository.Object);
            servicePackage.EnsurePackage(mockRepository.Object);
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert
            Assert.Equal(zipPackage, servicePackage.Package);
            context.Verify(s => s.GetReadStreamUri(It.IsAny<object>()), Times.Once());
            packageDownloader.Verify(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>()), Times.Once());
            mockRepository.Verify();
        }

        [Fact]
        public void EnsurePackageDownloadsUsesMachineCacheIfAvailable()
        {
            // Arrange
            var hashBytes = new byte[] { 1, 2, 3, 4 };
            var hash = Convert.ToBase64String(hashBytes);
            var zipPackage = PackageUtility.CreatePackage("A", "1.2");

            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<byte[]>())).Returns(hashBytes);

            var mockRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            mockRepository.Setup(r => r.GetPackages())
                          .Returns(new[] { zipPackage }.AsQueryable())
                          .Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = hash,
                HashProvider = hashProvider.Object,
            };

            // Act
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert
            Assert.Equal(zipPackage, servicePackage.Package);
            mockRepository.Verify();
        }

        [Fact]
        public void EnsurePackageDownloadsPackageIfCacheIsInvalid()
        {
            // Arrange
            byte[] hashBytes1 = new byte[] { 1, 2, 3, 4 }, hashBytes2 = new byte[] { 3, 4, 5, 6 };
            string hash1 = Convert.ToBase64String(hashBytes1), hash2 = Convert.ToBase64String(hashBytes2);
            var zipPackage1 = PackageUtility.CreatePackage("A", "1.2");
            var zipPackage2 = PackageUtility.CreatePackage("A", "1.2");

            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<byte[]>())).Returns(hashBytes1);


            var mockRepository = new MockPackageRepository();
            mockRepository.Add(zipPackage1);

            var uri = new Uri("http://nuget.org");
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>()))
                             .Returns(zipPackage2)
                             .Verifiable();

            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = hash1,
                HashProvider = hashProvider.Object,
                Downloader = packageDownloader.Object,
                Context = context.Object
            };

            // Act 1
            servicePackage.EnsurePackage(mockRepository);

            // Assert 1
            Assert.Equal(zipPackage1, servicePackage.Package);

            // Act 2
            servicePackage.PackageHash = hash2;
            servicePackage.EnsurePackage(mockRepository);

            // Assert 2
            Assert.Equal(zipPackage2, servicePackage.Package);
            Assert.True(mockRepository.Exists(zipPackage2));
            context.Verify();
            packageDownloader.Verify();

        }
    }
}
