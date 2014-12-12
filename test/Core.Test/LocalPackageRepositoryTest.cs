using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using NuGet.Test.Utility;

namespace NuGet.Test
{

    public class LocalPackageRepositoryTest
    {
        [Fact]
        public void GetPackageFilesOnlyDetectsFilesWithPackageExtension()
        {
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
        public void GetPackageFilesDetectsFilesInRootOrFirstLevelOfFolders()
        {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile("P1.nupkg");
            mockFileSystem.AddFile("bar.zip");
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"baz\P2.nupkg"));
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"A\B\P3.nupkg"));
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"A\P4.nupkg"));
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);

            // Act
            var files = repository.GetPackageFiles().ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Equal(PathFixUtility.FixPath(@"baz\P2.nupkg"), files[0]);
            Assert.Equal(PathFixUtility.FixPath(@"A\P4.nupkg"), files[1]);
            Assert.Equal("P1.nupkg", files[2]);
        }

        [Fact]
        public void GetPackagesOnlyRetrievesPackageFilesWhereLastModifiedIsOutOfDate()
        {
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
            Func<string, IPackage> openPackage = p =>
            {
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
        public void FindPackageMatchesExactVersionIfSideBySideIsDisabled()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(PathFixUtility.FixPath(@"A\A.nupkg"));

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false), fileSystem, enableCaching: false);
            var searchedPaths = new List<string>();
            Func<string, IPackage> openPackage = p =>
            {
                searchedPaths.Add(p);
                return PackageUtility.CreatePackage("A", "1.1");
            };

            // Act and Assert
            IPackage result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.0"));
            Assert.Null(result);
            Assert.Equal(PathFixUtility.FixPath(@"A\A.nupkg"), searchedPaths.Single());

            searchedPaths.Clear();
            result = repository.FindPackage(openPackage, "A", new SemanticVersion("0.8"));
            Assert.Null(result);
            Assert.Equal(PathFixUtility.FixPath(@"A\A.nupkg"), searchedPaths.Single());

            searchedPaths.Clear();
            result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.1"));
            Assert.Equal("A", result.Id);
            Assert.Equal(new SemanticVersion("1.1"), result.Version);
        }

        [Fact]
        public void FindPackageMatchesExactVersionIfSideBySideIsEnabled()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(PathFixUtility.FixPath(@"A.1.1\A.1.1.nupkg"));

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: true), fileSystem, enableCaching: false);
            var searchedPaths = new List<string>();
            Func<string, IPackage> openPackage = p =>
            {
                searchedPaths.Add(p);
                return PackageUtility.CreatePackage("A", "1.1");
            };

            // Act and Assert
            IPackage result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.0"));
            Assert.Null(result);
            Assert.False(searchedPaths.Any());

            result = repository.FindPackage(openPackage, "A", new SemanticVersion("0.8"));
            Assert.Null(result);
            Assert.False(searchedPaths.Any());

            result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.1"));
            Assert.Equal(PathFixUtility.FixPath(@"A.1.1\A.1.1.nupkg"), searchedPaths.Single());
            Assert.Equal("A", result.Id);
            Assert.Equal(new SemanticVersion("1.1"), result.Version);
        }

        [Fact]
        public void FindPackageVerifiesPackageFileExistsOnFileSystemWhenCaching()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"A.1.0.0.nupkg");

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: true), fileSystem, enableCaching: true);
            var searchedPaths = new List<string>();
            Func<string, IPackage> openPackage = p =>
            {
                searchedPaths.Add(p);
                return PackageUtility.CreatePackage("A", "1.0");
            };

            // Act - 1
            IPackage result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.0"));

            // Assert - 1
            Assert.NotNull(result);
            Assert.Equal("A", result.Id);
            Assert.Equal(new SemanticVersion("1.0"), result.Version);

            // Act - 2
            fileSystem.DeleteFile("A.1.0.0.nupkg");
            result = repository.FindPackage(openPackage, "A", new SemanticVersion("1.0"));

            // Assert - 2
            Assert.Null(result);
        }

        [Fact]
        public void AddPackageAddsFileToFileSystem()
        {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.AddPackage(package);

            // Assert
            Assert.True(mockFileSystem.FileExists(PathFixUtility.FixPath(@"A.1.0\A.1.0.nupkg")));
        }

        [Fact]
        public void RemovePackageRemovesPackageFileAndDirectoryAndRoot()
        {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"A.1.0\A.1.0.nupkg"));
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Equal(3, mockFileSystem.Deleted.Count);
            Assert.True(mockFileSystem.Deleted.Contains(mockFileSystem.Root));
            Assert.True(mockFileSystem.Deleted.Contains(Path.Combine(mockFileSystem.Root, "A.1.0")));
            Assert.True(mockFileSystem.Deleted.Contains(Path.Combine(mockFileSystem.Root, PathFixUtility.FixPath(@"A.1.0\A.1.0.nupkg"))));
        }

        [Fact]
        public void RemovePackageDoesNotRemovesRootIfNotEmpty()
        {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"A.1.0\A.1.0.nupkg"));
            mockFileSystem.AddFile(PathFixUtility.FixPath(@"B.1.0\B.1.0.nupkg"));
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Equal(2, mockFileSystem.Deleted.Count);
            Assert.True(mockFileSystem.Deleted.Contains(Path.Combine(mockFileSystem.Root, @"A.1.0")));
            Assert.True(mockFileSystem.Deleted.Contains(Path.Combine(mockFileSystem.Root, PathFixUtility.FixPath(@"A.1.0\A.1.0.nupkg"))));
        }

        [Fact]
        public void FindPackagesByIdReturnsEmptySequenceIfNoPackagesWithSpecifiedIdAreFound()
        {
            // Arramge
            var fileSystem = new MockFileSystem();
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var localPackageRepository = new LocalPackageRepository(pathResolver, fileSystem);

            // Act
            var packages = localPackageRepository.FindPackagesById("Foo");

            // Assert
            Assert.Empty(packages);
        }

        [Fact]
        public void FindPackagesByIdFindsPackagesWithSpecifiedId()
        {
            // Arramge
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(PathFixUtility.FixPath(@"Foo.1.0\Foo.1.0.nupkg"));
            fileSystem.AddFile(PathFixUtility.FixPath(@"Foo.2.0.0\Foo.2.0.0.nupkg"));
            var foo_10 = PackageUtility.CreatePackage("Foo", "1.0");
            var foo_20 = PackageUtility.CreatePackage("Foo", "2.0.0");

            var package_dictionary = new Dictionary<String, IPackage>
			{
				{ PathFixUtility.FixPath(@"Foo.1.0\Foo.1.0.nupkg"), foo_10},
				{ PathFixUtility.FixPath(@"Foo.2.0.0\Foo.2.0.0.nupkg"), foo_20}
			};

            var localPackageRepository = new MockLocalRepository(fileSystem, path =>
                {
                    IPackage retval;
                    package_dictionary.TryGetValue(path, out retval);
                    return retval;
                });

            // Act
            var packages = localPackageRepository.FindPackagesById("Foo").ToList();

            // Assert
            Assert.Equal(new[] { foo_10, foo_20 }, packages);
        }

        [Fact]
        public void FindPackagesByIdIgnoresPartialIdMatches()
        {
            // Arramge
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(PathFixUtility.FixPath(@"Foo.1.0\Foo.1.0.nupkg"));
            fileSystem.AddFile(PathFixUtility.FixPath(@"Foo.2.0.0\Foo.2.0.0.nupkg"));
            fileSystem.AddFile(PathFixUtility.FixPath(@"Foo.Baz.2.0.0\Foo.Baz.2.0.0.nupkg"));
            var foo_10 = PackageUtility.CreatePackage("Foo", "1.0");
            var foo_20 = PackageUtility.CreatePackage("Foo", "2.0.0");
            var fooBaz_20 = PackageUtility.CreatePackage("Foo.Baz", "2.0.0");

            var package_dictionary = new Dictionary<string, IPackage>(){
					{ PathFixUtility.FixPath(@"Foo.1.0\Foo.1.0.nupkg"),foo_10},
					{ PathFixUtility.FixPath(@"Foo.2.0.0\Foo.2.0.0.nupkg"), foo_20},
					{ PathFixUtility.FixPath(@"Foo.Baz.2.0.0\Foo.Baz.2.0.0.nupkg"), fooBaz_20}
			};

            var localPackageRepository = new MockLocalRepository(fileSystem, path =>
            {
                IPackage retval;
                package_dictionary.TryGetValue(path, out retval);
                return retval;
            });

            // Act
            var packages = localPackageRepository.FindPackagesById("Foo").ToList();

            // Assert
            Assert.Equal(new[] { foo_10, foo_20 }, packages);
        }

        private static DateTimeOffset GetDateTimeOffset(int seconds)
        {
            return new DateTimeOffset(1000, 10, 1, 0, 0, seconds, TimeSpan.Zero);
        }

        private class MockLocalRepository : LocalPackageRepository
        {
            private readonly Func<string, IPackage> _openPackage;

            public MockLocalRepository(IFileSystem fileSystem, Func<string, IPackage> openPackage = null)
                : base(new DefaultPackagePathResolver(fileSystem), fileSystem)
            {
                _openPackage = openPackage;
            }

            protected override IPackage OpenPackage(string path)
            {
                return _openPackage(path);
            }
        }
    }
}
