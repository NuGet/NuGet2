using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
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
            Assert.False(((IPackage)servicePackage).DependencySets.Any());
        }

        [Fact]
        public void NullDependenciesStringReturnsEmptyDependenciesCollection()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Assert
            Assert.False(((IPackage)servicePackage).DependencySets.Any());
        }

        [Fact]
        public void DependenciesStringWithExtraSpaces()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "      A   :   1.3 | B :  [2.4, 5.0)   ";

            List<PackageDependencySet> dependencySets = ((IPackage)servicePackage).DependencySets.ToList();

            // Assert
            Assert.Equal(1, dependencySets.Count);

            List<PackageDependency> dependencies = dependencySets[0].Dependencies.ToList();            
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
        public void DependenciesStringWithTargetFrameworks()
        {
            // Arrange
            var servicePackage = new DataServicePackage();

            // Act
            servicePackage.Dependencies = "A:1.3:net40|B:[2.4, 5.0):sl5|C|D::winrt45|E:1.0";

            List<PackageDependencySet> dependencySets = ((IPackage)servicePackage).DependencySets.ToList();

            // Assert
            Assert.Equal(4, dependencySets.Count);

            Assert.Equal(1, dependencySets[0].Dependencies.Count);
            Assert.Equal(new FrameworkName(".NETFramework, Version=4.0"), dependencySets[0].TargetFramework);
            Assert.Equal("A", dependencySets[0].Dependencies.ElementAt(0).Id);
            Assert.Equal("A", dependencySets[0].Dependencies.ElementAt(0).Id);

            Assert.Equal(1, dependencySets[1].Dependencies.Count);
            Assert.Equal(new FrameworkName("Silverlight, Version=5.0"), dependencySets[1].TargetFramework);
            Assert.Equal("B", dependencySets[1].Dependencies.ElementAt(0).Id);

            Assert.Equal(2, dependencySets[2].Dependencies.Count);
            Assert.Null(dependencySets[2].TargetFramework);
            Assert.Equal("C", dependencySets[2].Dependencies.ElementAt(0).Id);
            Assert.Null(dependencySets[2].Dependencies.ElementAt(0).VersionSpec);
            Assert.Equal("E", dependencySets[2].Dependencies.ElementAt(1).Id);
            Assert.NotNull(dependencySets[2].Dependencies.ElementAt(1).VersionSpec);

            Assert.Equal(1, dependencySets[3].Dependencies.Count);
            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), dependencySets[3].TargetFramework);
            Assert.Equal("D", dependencySets[3].Dependencies.ElementAt(0).Id);
            Assert.Null(dependencySets[3].Dependencies.ElementAt(0).VersionSpec);
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
            servicePackage.EnsurePackage(mockRepository);
            servicePackage.EnsurePackage(mockRepository);

            // Assert
            Assert.Equal(zipPackage, servicePackage._package);
            context.Verify(s => s.GetReadStreamUri(It.IsAny<object>()), Times.Once());
            packageDownloader.Verify(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>()), Times.Once());
            Assert.True(mockRepository.Exists(zipPackage));
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

            var mockRepository = new MockPackageRepository();
            mockRepository.Add(zipPackage);

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = hash,
                HashProvider = hashProvider.Object,
            };

            // Act
            servicePackage.EnsurePackage(mockRepository);

            // Assert
            Assert.Equal(zipPackage, servicePackage._package);
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

            var mockRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            var lookup = mockRepository.As<IPackageLookup>();
            lookup.Setup(s => s.FindPackage("A", new SemanticVersion("1.2")))
                  .Returns(zipPackage1);
            lookup.Setup(s => s.Exists("A", new SemanticVersion("1.2")))
                  .Returns(true);
            mockRepository.Setup(s => s.AddPackage(zipPackage2)).Verifiable();

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
                PackageHashAlgorithm = "SHA512",
                HashProvider = hashProvider.Object,
                Downloader = packageDownloader.Object,
                Context = context.Object
            };

            // Act 1
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert 1
            Assert.Equal(zipPackage1, servicePackage._package);
            context.Verify(c => c.GetReadStreamUri(It.IsAny<object>()), Times.Never());

            // Act 2
            servicePackage.PackageHash = hash2;
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert 2
            Assert.Equal(zipPackage2, servicePackage._package);
            mockRepository.Verify();
            context.Verify();
            packageDownloader.Verify();

        }
    }
}
