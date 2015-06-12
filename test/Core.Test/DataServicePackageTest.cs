using System;
using System.Collections.Generic;
using System.IO;
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
        public void MinClientVersionReturnsParsedValue()
        {
            // Arrange
            var package = new DataServicePackage
            {
                MinClientVersion = "2.4.0.1"
            };

            // Act
            Version minClientVersion = (package as IPackageMetadata).MinClientVersion;

            // Assert
            Assert.Equal("2.4.0.1", minClientVersion.ToString());
        }

        [Fact]
        public void MinClientVersionReturnsNullValue()
        {
            // Arrange
            var package = new DataServicePackage();

            // Act
            Version minClientVersion = (package as IPackageMetadata).MinClientVersion;

            // Assert
            Assert.Null(minClientVersion);
        }

        [Fact]
        public void EnsurePackageDownloadsThePackageAndSetsItsIdAndVersionAsperNuspec()
        {            
            // Arrange
            var uri = new Uri("http://nuget.org");
            var mockRepository = new Mock<MockPackageRepository>().As<IPackageCacheRepository>();
            mockRepository.Setup(s => s.InvokeOnPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), It.IsAny<Action<Stream>>())).Returns(false);

            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(new Action<Uri, IPackageMetadata, Stream>(
                                 (url, metadata, stream) => PackageUtility.CreateSimplePackageStream("LowerCase", "1.2.0").CopyTo(stream)))
                             .Verifiable();
            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            byte[] hash1 = new byte[] { 1, 2, 3, 4 };
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) => hash1);

            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            // Setup a different casing for package Id and non-normalized version for the data service package.
            var servicePackage = new DataServicePackage
            {
                Id = "Lowercase",
                Version = "1.2",
                PackageHash = Convert.ToBase64String(hash1),
                Downloader = packageDownloader.Object,
                HashProvider = hashProvider.Object,
                Context = context.Object
            };

            // Act
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert
            context.Verify();
            packageDownloader.Verify();

            var foundPackage = servicePackage._package;
            Assert.NotNull(foundPackage);
            Assert.True(foundPackage is ZipPackage);

            // Ensure the package Id and version matches the one that is present in nuspec.
            Assert.Equal(foundPackage.Id, "LowerCase", StringComparer.Ordinal);
            Assert.Equal(foundPackage.Version.ToString(), "1.2.0", StringComparer.Ordinal);
        }

        [Fact]
        public void EnsurePackageDownloadsThePackageIfItIsNotCachedInMemoryOnInMachineCache()
        {
            // Arrange
            var zipPackage = PackageUtility.CreatePackage("A", "1.2");
            var uri = new Uri("http://nuget.org");
            var mockRepository = new MockPackageCacheRepository(true);
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(() => mockRepository.AddPackage(zipPackage))
                             .Verifiable();

            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) => new byte[] { 1, 2, 3, 4 });

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
                Downloader = packageDownloader.Object,
                HashProvider = hashProvider.Object,
                Context = context.Object,
            };

            // Act
            servicePackage.EnsurePackage(mockRepository);

            // Assert
            context.Verify();
            packageDownloader.Verify();
            Assert.True(mockRepository.Exists(zipPackage));
        }

        [Fact]
        public void EnsurePackageStorePackageInMemoryIfMachineCacheIsNotAvailable()
        {
            // Arrange
            var uri = new Uri("http://nuget.org");
            var mockRepository = new Mock<MockPackageRepository>().As<IPackageCacheRepository>();
            mockRepository.Setup(s => s.InvokeOnPackage(It.IsAny<string>(), It.IsAny<SemanticVersion>(), It.IsAny<Action<Stream>>())).Returns(false);

            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(new Action<Uri, IPackageMetadata, Stream>(
                                 (url, metadata, stream) => PackageUtility.CreateSimplePackageStream("A", "1.2").CopyTo(stream)))
                             .Verifiable();
            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            byte[] hash1 = new byte[] { 1, 2, 3, 4 };
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) => hash1);

            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = Convert.ToBase64String(hash1),
                Downloader = packageDownloader.Object,
                HashProvider = hashProvider.Object,
                Context = context.Object
            };

            // Act
            servicePackage.EnsurePackage(mockRepository.Object);

            // Assert
            context.Verify();
            packageDownloader.Verify();

            var foundPackage = servicePackage._package;
            Assert.NotNull(foundPackage);
            Assert.True(foundPackage is ZipPackage);
        }

        [Fact]
        public void EnsurePackageDownloadsUsesInMemoryCachedInstanceOnceDownloaded()
        {
            // Arrange
            var zipPackage = PackageUtility.CreatePackage("A", "1.2");
            var uri = new Uri("http://nuget.org");
            var mockRepository = new MockPackageCacheRepository(true);
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(() => mockRepository.AddPackage(zipPackage))
                             .Verifiable();
            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) => new byte[] { 1, 2, 3, 4 });
            
            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
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
            packageDownloader.Verify(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()), Times.Once());
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
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns(hashBytes);

            var mockRepository = new MockPackageCacheRepository(true);
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
            byte[] hashBytes1 = new byte[] { 1, 2, 3, 4 };
            byte[] hashBytes2 = new byte[] { 3, 4, 5, 6 };
            string hash1 = Convert.ToBase64String(hashBytes1);
            string hash2 = Convert.ToBase64String(hashBytes2);
            MemoryStream stream1 = new MemoryStream(new byte[] { 1 });
            MemoryStream stream2 = new MemoryStream(new byte[] { 2 });

            var mockPackage = new Mock<IPackage>(MockBehavior.Strict) { CallBase = true };
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Listed).Returns(true);
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            mockPackage.Setup(m => m.GetStream()).Returns(stream1);

            var zipPackage1 = mockPackage.Object;

            var mockPackage2 = new Mock<IPackage>(MockBehavior.Strict) { CallBase = true };
            mockPackage2.Setup(m => m.Id).Returns("A");
            mockPackage2.Setup(m => m.Listed).Returns(true);
            mockPackage2.Setup(m => m.Version).Returns(new SemanticVersion("1.2"));
            mockPackage2.Setup(m => m.GetStream()).Returns(stream2);

            var zipPackage2 = mockPackage2.Object;

            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) =>
                {
                    if (stream == stream1)
                    {
                        return hashBytes1;
                    }
                    else if (stream == stream2)
                    {
                        return hashBytes2;
                    }

                    return null;
                });

            var mockRepository = new Mock<IPackageCacheRepository>(MockBehavior.Strict);
            var lookup = mockRepository.As<IPackageLookup>();
            lookup.Setup(s => s.FindPackage("A", new SemanticVersion("1.2")))
                  .Returns(zipPackage1);
            lookup.Setup(s => s.Exists("A", new SemanticVersion("1.2")))
                  .Returns(true);

            var uri = new Uri("http://nuget.org");
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(() =>
                                {
                                    lookup.Setup(s => s.FindPackage("A", new SemanticVersion("1.2")))
                                           .Returns(zipPackage2);
                                })
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

            mockRepository.Setup(s => s.InvokeOnPackage("A", new SemanticVersion("1.2"), It.IsAny<Action<Stream>>()))
                .Callback(() =>
                {
                    using (var stream = new MemoryStream())
                    {
                        packageDownloader.Object.DownloadPackage(servicePackage.DownloadUrl, servicePackage, stream);
                    }
                })
                .Returns(true);

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
            context.Verify();
            packageDownloader.Verify();
        }

        [Fact(Skip="Hash checking is disabled because it can fail.")]
        public void EnsurePackageDownloadsFailsIfHashIsIncorrect()
        {
            // Arrange
            byte[] hashBytes1 = new byte[] { 1, 2, 3, 4 };
            string hash1 = Convert.ToBase64String(hashBytes1);
            MemoryStream stream1 = new MemoryStream(new byte[] { 1 });

            var mockPackage = new Mock<IPackage>(MockBehavior.Strict) { CallBase = true };
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Listed).Returns(true);
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            mockPackage.Setup(m => m.GetStream()).Returns(stream1);

            var zipPackage1 = mockPackage.Object;

            var hashProvider = new Mock<IHashProvider>(MockBehavior.Strict);
            hashProvider.Setup(h => h.CalculateHash(It.IsAny<Stream>())).Returns<Stream>((stream) => hashBytes1);

            var mockRepository = new Mock<IPackageCacheRepository>(MockBehavior.Strict);
            var lookup = mockRepository.As<IPackageLookup>();
            lookup.Setup(s => s.FindPackage("A", new SemanticVersion("1.2")))
                  .Returns(zipPackage1);
            lookup.Setup(s => s.Exists("A", new SemanticVersion("1.2")))
                  .Returns(true);

            var uri = new Uri("http://nuget.org");
            var packageDownloader = new Mock<PackageDownloader>();
            packageDownloader.Setup(d => d.DownloadPackage(uri, It.IsAny<IPackageMetadata>(), It.IsAny<Stream>()))
                             .Callback(() =>
                                {
                                    lookup.Setup(s => s.FindPackage("A", new SemanticVersion("1.2")))
                                           .Returns(zipPackage1);
                                })
                             .Verifiable();

            var context = new Mock<IDataServiceContext>();
            context.Setup(c => c.GetReadStreamUri(It.IsAny<object>())).Returns(uri).Verifiable();

            var servicePackage = new DataServicePackage
            {
                Id = "A",
                Version = "1.2",
                PackageHash = "IMPOSSIBLEHASH",
                PackageHashAlgorithm = "SHA512",
                HashProvider = hashProvider.Object,
                Downloader = packageDownloader.Object,
                Context = context.Object
            };

            mockRepository.Setup(s => s.InvokeOnPackage("A", new SemanticVersion("1.2"), It.IsAny<Action<Stream>>()))
                .Callback(() =>
                {
                    using (var stream = new MemoryStream())
                    {
                        packageDownloader.Object.DownloadPackage(servicePackage.DownloadUrl, servicePackage, stream);
                    }
                })
                .Returns(true);

            // Act & Assert
            Assert.Throws(typeof(InvalidOperationException), () => servicePackage.EnsurePackage(mockRepository.Object));
        }
    }
}