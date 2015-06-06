using Moq;
using NuGet.Test.Mocks;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.Core
{
    public class LocalPackageRepositoryTest : IDisposable
    {
        private static readonly string _testRunDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [Theory]
        [InlineData("Test", "1.2", "Test.1.2.nupkg")]
        [InlineData("Test", "1.2", "Test.1.2.0.nupkg")]
        [InlineData("Test", "1.2", "Test.1.2.0.0.nupkg")]
        [InlineData("Test", "1.2.3.4", "Test.1.2.3.4.nupkg")]
        [InlineData("Test", "1.2.3", "Test.1.2.3.nupkg")]
        [InlineData("Test", "1.2.3", "Test.1.2.3.0.nupkg")]
        [InlineData("Test", "1.4.0-alpha", "Test.1.4.0-alpha.nupkg")]
        [InlineData("Test", "1.4.0-alpha", "Test.1.4-alpha.nupkg")]
        [InlineData("Test", "1.4.0-alpha", @"subdir\Test.1.4-alpha.nupkg")]
        [InlineData("Test", "1.4.0-alpha", @"subdir2\Test.1.4.0-alpha.nupkg")]
        public void FindPackageReturnsPackageWithVersionedFileNameAtRoot(string id, string version, string packageName)
        {
            // Arrange
            var repositoryRoot = CreatePackage(id, version, packageName);
            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var findPackage = repository.FindPackage(id, new SemanticVersion(version));

            // Assert
            Assert.True(findPackage is OptimizedZipPackage);
            AssertPackage(id, version, findPackage);
        }

        [Theory]
        [InlineData("Test", "1.4.0-alpha", @"Test.nupkg")]
        [InlineData("Test", "1.3.4.5", @"subdir\Test.nupkg")]
        public void FindPackageReturnsPackageWithUnVersionedFileNameWhenUsingVersionlessPathResolver(string id, string version, string packageName)
        {
            // Arrange
            var repositoryRoot = CreatePackage(id, version, packageName);
            var fileSystem = new PhysicalFileSystem(repositoryRoot);
            var pathResolver = new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: false);
            var repository = new LocalPackageRepository(pathResolver, fileSystem);

            // Act
            var findPackage = repository.FindPackage(id, new SemanticVersion(version));

            // Assert
            Assert.True(findPackage is OptimizedZipPackage);
            AssertPackage(id, version, findPackage);
        }

        [Fact]
        public void FindPackageMatchesExactVersion()
        {
            // Arrange
            string id = "Test";
            string version = "1.3.1.0";
            string packageName = "Test.1.3.1.nupkg";
            var repositoryRoot = CreatePackage(id, version, packageName);
            CreatePackage(id, "1.3.4", "Test.nupkg", repositoryRoot);

            // Create dummy files. Attempting to open these packages would throw, so we know we're not opening it.
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.2.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.31.nupkg"), "");

            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var findPackage = repository.FindPackage(id, new SemanticVersion(version));

            // Assert
            AssertPackage(id, version, findPackage);
        }

        [Fact]
        public void FindPackageReturnsPackagesFromNestedDirectories()
        {
            // Arrange
            string id = "Test";
            string version = "1.3.1.0";
            string packageName = @"SubDir\Test.1.3.1.nupkg";
            var repositoryRoot = CreatePackage(id, version, packageName);
            CreatePackage(id, "1.3.4", "Test.nupkg", repositoryRoot);

            // Create dummy files. Attempting to open these packages would throw, so we know we're not opening it.
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.2.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.31.nupkg"), "");

            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var findPackage = repository.FindPackage(id, new SemanticVersion(version));

            // Assert
            Assert.True(findPackage is OptimizedZipPackage);
            AssertPackage(id, version, findPackage);
        }

        [Fact]
        public void FindPackageReturnsPackagesWithDotInId()
        {
            // Arrange
            var repositoryRoot = CreatePackage("Test", "1.0.0", @"Test.1.0.0.nupkg");
            CreatePackage("Test", "1.2.0", @"SubDir\Test.1.0.0.nupkg");
            CreatePackage("test.extensions", "1.0", "Test.Extensions.1.0.0.0.nupkg", repositoryRoot);
            CreatePackage("test.extensions", "1.3.0-alpha", "Test.Extensions.1.3.0-alpha.nupkg", repositoryRoot);

            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var result1 = repository.FindPackage("test.extensions", new SemanticVersion("1.0"));
            var result2 = repository.FindPackage("test.extensions", new SemanticVersion("1.3-alpha"));

            // Assert
            Assert.True(result1 is OptimizedZipPackage);
            Assert.True(result2 is OptimizedZipPackage);
            AssertPackage("test.extensions", "1.0", result1);
            AssertPackage("test.extensions", "1.3.0-alpha", result2);
        }

        [Fact]
        public void FindPackageReturnsNullIfItCannotMatchExactVersion()
        {
            // Arrange
            string id = "Test";
            string version = "1.3.1.0";
            string packageName = @"SubDir\Test.1.3.1.nupkg";
            var repositoryRoot = CreatePackage(id, version, packageName);
            CreatePackage(id, "1.3.4", "Test.nupkg", repositoryRoot);

            // Create dummy files. Attempting to open these packages would throw, so we know we're not opening it.
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.2.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.3.nupkg"), "");
            File.WriteAllText(Path.Combine(repositoryRoot, "Test.1.31.nupkg"), "");

            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var findPackage = repository.FindPackage(id, new SemanticVersion("1.5"));

            // Assert
            Assert.Null(findPackage);
        }

        [Fact]
        public void FindPackagesByIdFindPackagesMatchingExactId()
        {
            // Arrange
            string id = "Test";
            var repositoryRoot = CreatePackage(id, version: "1.2", packageFileName: "Test.1.2.nupkg");
            CreatePackage(id, version: "1.3", packageFileName: "Test.1.3.nupkg", rootDir: repositoryRoot);
            CreatePackage(id, version: "2.0.0.9200-alpha", packageFileName: "Test.2.0.0.9200.nupkg", rootDir: repositoryRoot);

            IPackageRepository repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var packages = repository.FindPackagesById(id).ToList();

            // Assert
            foreach (var p in packages)
            {
                Assert.True(p is OptimizedZipPackage);
            }

            Assert.Equal(3, packages.Count);
            Assert.Equal("1.2", packages[0].Version.ToString());
            Assert.Equal("1.3", packages[1].Version.ToString());
            Assert.Equal("2.0.0.9200-alpha", packages[2].Version.ToString());
        }

        [Fact]
        public void FindPackagesByIdIgnoresPackagesThatDoNotMatchId()
        {
            // Arrange
            string id = "Test";
            var repositoryRoot = CreatePackage(id, version: "1.2", packageFileName: "Test.1.2.nupkg");
            CreatePackage(id, version: "1.3", packageFileName: "TeST.1.3.nupkg", rootDir: repositoryRoot);
            CreatePackage(id, version: "2.0.0.9200-alpha", packageFileName: "TEst.2.0.0.9200.nupkg", rootDir: repositoryRoot);
            CreatePackage("Test2", version: "2.0", packageFileName: "Test2.2.0.nupkg", rootDir: repositoryRoot);
            File.WriteAllBytes(Path.Combine(repositoryRoot, "NotTest.1.0.nupkg"), new byte[0]);

            IPackageRepository repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var packages = repository.FindPackagesById(id).ToList();

            // Assert
            Assert.Equal(3, packages.Count);
            Assert.Equal("1.2", packages[0].Version.ToString());
            Assert.Equal("1.3", packages[1].Version.ToString());
            Assert.Equal("2.0.0.9200-alpha", packages[2].Version.ToString());
        }

        [Fact]
        public void FindPackagesByUsesIdFromManifestToValidateIdMatches()
        {
            // Arrange
            string id = "Test";
            var repositoryRoot = CreatePackage(id, version: "1.2", packageFileName: "Test.1.2.nupkg");
            CreatePackage(id, version: "1.3", packageFileName: "TeST.1.3.nupkg", rootDir: repositoryRoot);
            CreatePackage("Blah", version: "2.0.0", packageFileName: "Test.2.0.0.nupkg", rootDir: repositoryRoot);

            IPackageRepository repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var packages = repository.FindPackagesById(id).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal("1.2", packages[0].Version.ToString());
            Assert.Equal("1.3", packages[1].Version.ToString());
        }

        [Fact]
        public void GetPackageReturnsPackagesFromNestedDirectories()
        {
            // Arrange
            string id = "Test";
            string version = "1.3.1.0";
            string packageName = @"SubDir\Test.1.3.1.nupkg";
            var repositoryRoot = CreatePackage(id, version, packageName);
            CreatePackage(id, "1.3.4", "Test.nupkg", repositoryRoot);
            CreatePackage("Foo", "1.4", @"SubDir2\Test.1.4.nupkg", repositoryRoot);

            var repository = new LocalPackageRepository(repositoryRoot);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(3, packages.Count);
            AssertPackage("Test", "1.3.1.0", packages[0]);
            AssertPackage("Foo", "1.4", packages[1]);
            AssertPackage("Test", "1.3.4", packages[2]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindPackagesByIdThrowsIfIdIsNullOrEmpty(string id)
        {
            // Arrange
            var repository = new LocalPackageRepository(Mock.Of<IPackagePathResolver>(), Mock.Of<IFileSystem>());

            // Act and Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => repository.FindPackagesById(id), "packageId");
        }

        [Fact]
        public void OpenPackagePrintsPathToPackageIfItCannotBeRead()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Foo.nupkg");
            var pathResolver = new DefaultPackagePathResolver(fileSystem);
            var repository = new LocalPackageRepository(pathResolver, fileSystem);

            // Act and Assert
            ExceptionAssert.Throws<InvalidDataException>(() => repository.OpenPackage(@"Foo.nupkg"), "Unable to read package from path 'Foo.nupkg'.");
        }

        private static void AssertPackage(string id, string version, IPackage findPackage)
        {
            Assert.NotNull(findPackage);
            Assert.Equal(id, findPackage.Id);
            Assert.Equal(version, findPackage.Version.ToString());
        }

        private static string CreatePackage(string id = "Test", string version = "1.2", string packageFileName = "Test.1.2.nupkg", string rootDir = null)
        {
            rootDir = rootDir ?? Path.Combine(_testRunDirectory, Path.GetRandomFileName());
            string packagePath = Path.Combine(rootDir, packageFileName);
            var packageBuilder = new PackageBuilder
            {
                Id = id,
                Version = new SemanticVersion(version),
                Description = "Test desc"
            };

            var dependencies = new PackageDependency("Dummy");
            packageBuilder.DependencySets.Add(new PackageDependencySet(null, new[] { dependencies }));
            packageBuilder.Authors.Add("test author");

            Directory.CreateDirectory(Path.GetDirectoryName(packagePath));
            using (var fileStream = File.Create(packagePath))
            {
                packageBuilder.Save(fileStream);
            }

            return rootDir;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testRunDirectory, recursive: true);
            }
            catch { }
        }
    }
}