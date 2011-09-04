using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    
    public class LocalPackageRepositoryTest {
        [Fact]
        public void GetPackageFilesOnlyDetectsFilesWithPackageExtension() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile("foo.nupkg");
            mockFileSystem.AddFile("bar.zip");

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);

            // Act
            var files = repository.GetPackageFiles().ToList();

            // Assert
            Assert.Equal(1, files.Count);
            Assert.Equal("foo.nupkg", files[0]);
        }

        [Fact]
        public void GetPackageFilesDetectsFilesInRootOrFirstLevelOfFolders() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile("P1.nupkg");
            mockFileSystem.AddFile("bar.zip");
            mockFileSystem.AddFile(@"baz\P2.nupkg");
            mockFileSystem.AddFile(@"A\B\P3.nupkg");
            mockFileSystem.AddFile(@"A\P4.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);

            // Act
            var files = repository.GetPackageFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(@"baz\P2.nupkg", files[0]);
            Assert.Equal(@"A\P4.nupkg", files[1]);
            Assert.Equal("P1.nupkg", files[2]);
        }

        [Fact]
        public void GetPackagesOnlyRetrievesPackageFilesWhereLastModifiedIsOutOfDate() {
            // Arrange
            var mockFileSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var lastModified = new Dictionary<string, DateTimeOffset>();
            mockFileSystem.Setup(m => m.GetLastModified("P1.nupkg")).Returns(() => lastModified["P1.nupkg"]);
            mockFileSystem.Setup(m => m.GetLastModified("P2.nupkg")).Returns(() => lastModified["P2.nupkg"]);
            mockFileSystem.Object.AddFile("P1.nupkg");
            mockFileSystem.Object.AddFile("P2.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem.Object),
                                                        mockFileSystem.Object);
            var results = new List<string>();
            Func<string, IPackage> openPackage = p => {
                results.Add(p);
                string id = Path.GetFileNameWithoutExtension(p);
                return PackageUtility.CreatePackage(id, "1.0");
            };

            // Populate cache
            lastModified["P1.nupkg"] = GetDateTimeOffset(seconds: 30);
            lastModified["P2.nupkg"] = GetDateTimeOffset(seconds: 30);
            repository.GetPackages(openPackage).ToList();

            // Verify that both packages have been created from the file system
            Assert.Equal(2, results.Count);
            results.Clear();

            // Act
            lastModified["P1.nupkg"] = GetDateTimeOffset(seconds: 35);
            lastModified["P2.nupkg"] = GetDateTimeOffset(seconds: 30);
            repository.GetPackages(openPackage).ToList();

            // Assert
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0], "P1.nupkg");
        }

        [Fact]
        public void FindPackageMatchesExactVersionIfSideBySideIsDisabled() {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"A\A.nupkg");

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false), fileSystem, enableCaching: false);
            var searchedPaths = new List<string>();
            Func<string, IPackage> openPackage = p => {
                searchedPaths.Add(p);
                string id = Path.GetFileNameWithoutExtension(p);
                return PackageUtility.CreatePackage("A", "1.1");
            };

            // Act and Assert
            IPackage result = repository.FindPackage(openPackage, "A", new Version("1.0"));
            Assert.Null(result);
            Assert.Equal(@"A\A.nupkg", searchedPaths.Single());

            searchedPaths.Clear();
            result = repository.FindPackage(openPackage, "A", new Version("0.8"));
            Assert.Null(result);
            Assert.Equal(@"A\A.nupkg", searchedPaths.Single());

            searchedPaths.Clear();
            result = repository.FindPackage(openPackage, "A", new Version("1.1"));
            Assert.Equal("A", result.Id);
            Assert.Equal(new Version("1.1"), result.Version);
        }

        [Fact]
        public void FindPackageMatchesExactVersionIfSideBySideIsEnabled() {
            // Arrange
            var fileSystem = new Mock<MockProjectSystem> { CallBase = true };
            fileSystem.Setup(c => c.FileExists(It.Is<string>(a => a.Equals(@"A.1.0\A.1.0.nupkg")))).Returns(false);
            fileSystem.Setup(c => c.FileExists(It.Is<string>(a => a.Equals(@"A.0.8\A.0.8.nupkg")))).Returns(false);
            fileSystem.Setup(c => c.FileExists(It.Is<string>(a => a.Equals(@"A.1.1\A.1.1.nupkg")))).Returns(true);

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem.Object, useSideBySidePaths: true), fileSystem.Object, enableCaching: false);
            var searchedPaths = new List<string>();
            Func<string, IPackage> openPackage = p => {
                searchedPaths.Add(p);
                string id = Path.GetFileNameWithoutExtension(p);
                return PackageUtility.CreatePackage("A", "1.1");
            };

            // Act and Assert
            IPackage result = repository.FindPackage(openPackage, "A", new Version("1.0"));
            Assert.Null(result);
            Assert.False(searchedPaths.Any());

            result = repository.FindPackage(openPackage, "A", new Version("0.8"));
            Assert.Null(result);
            Assert.False(searchedPaths.Any());

            result = repository.FindPackage(openPackage, "A", new Version("1.1"));
            Assert.Equal(@"A.1.1\A.1.1.nupkg", searchedPaths.Single());
            Assert.Equal("A", result.Id);
            Assert.Equal(new Version("1.1"), result.Version);

            fileSystem.Verify();
        }

        [Fact]
        public void AddPackageAddsFileToFileSystem() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.AddPackage(package);

            // Assert
            Assert.True(mockFileSystem.FileExists(@"A.1.0\A.1.0.nupkg"));
        }

        [Fact]
        public void RemovePackageRemovesPackageFileAndDirectoryAndRoot() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(@"A.1.0\A.1.0.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Equal(3, mockFileSystem.Deleted.Count);
            Assert.True(mockFileSystem.Deleted.Contains(""));
            Assert.True(mockFileSystem.Deleted.Contains("A.1.0"));
            Assert.True(mockFileSystem.Deleted.Contains(@"A.1.0\A.1.0.nupkg"));
        }

        [Fact]
        public void RemovePackageDoesNotRemovesRootIfNotEmpty() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(@"A.1.0\A.1.0.nupkg");
            mockFileSystem.AddFile(@"B.1.0\B.1.0.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Equal(2, mockFileSystem.Deleted.Count);
            Assert.True(mockFileSystem.Deleted.Contains("A.1.0"));
            Assert.True(mockFileSystem.Deleted.Contains(@"A.1.0\A.1.0.nupkg"));
        }

        private static DateTimeOffset GetDateTimeOffset(int seconds) {
            return new DateTimeOffset(1000, 10, 1, 0, 0, seconds, TimeSpan.Zero);
        }
    }
}
