using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class LazyLocalPackageRepositoryTest
    {
        [Fact]
        public void CreateRepository_ReturnsLocalPackageRepository_IfThereAreNupkgsInRootOfTheFileSystem()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"Foo.1.0.nupkg");
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nuspec"));
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var localPackageRepo = repository.Repository;

            // Assert
            Assert.IsType<LocalPackageRepository>(localPackageRepo);
        }

        [Fact]
        public void CreateRepository_ReturnsLocalPackageRepository_IfThereAreNupkgsInDirectoryOneLevelDeep()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "Foo.1.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nuspec"));
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var localPackageRepo = repository.Repository;

            // Assert
            Assert.IsType<LocalPackageRepository>(localPackageRepo);
        }

        [Fact]
        public void CreateRepository_ReturnsLocalPackageRepository_IfThereAreNuspecsInDirectoryOneLevelDeep()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "Bar.1.0.nuspec"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0", "Foo.1.0.nuspec"));
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var localPackageRepo = repository.Repository;

            // Assert
            Assert.IsType<LocalPackageRepository>(localPackageRepo);
        }

        [Fact]
        public void CreateRepository_ReturnsExpandedPackageRepository_IfThereArNoNupkgInTheTopLevelDirectoryOrOneLevelDeep()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.1.0.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"));

            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var localPackageRepo = repository.Repository;

            // Assert
            Assert.IsType<ExpandedPackageRepository>(localPackageRepo);
        }

        [Fact]
        public void PackageLookupOperationsAreDeferredToUnderlyingLocalPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var package = GetPackage();
            fileSystem.AddFile("MyPackage.1.0.0-beta2.nupkg", package.GetStream);
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var getPackagesResult = repository.GetPackages();
            var findPackagesByIdResult = repository.FindPackagesById("MyPackage");
            var findPackageResult = repository.FindPackage("MyPackage", new SemanticVersion("1.0.0-beta2"));

            // Assert
            var actual = Assert.Single(getPackagesResult);
            Assert.Equal(package.Id, actual.Id);
            Assert.Equal(package.Version, actual.Version);

            actual = Assert.Single(findPackagesByIdResult);
            Assert.Equal(package.Id, actual.Id);
            Assert.Equal(package.Version, actual.Version);

            Assert.Equal(package.Id, findPackageResult.Id);
            Assert.Equal(package.Version, findPackageResult.Version);
        }

        [Fact]
        public void PackageAddAndRemovalOperationsAreDeferredToUnderlyingLocalPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var package = GetPackage();
            var repository = new LazyLocalPackageRepository(fileSystem);
            var expectedPackagePath = Path.Combine("MyPackage.1.0.0-beta2", "MyPackage.1.0.0-beta2.nupkg");

            // Act - 1
            repository.AddPackage(package);

            // Assert - 1
            Assert.True(package.GetStream().ContentEquals(fileSystem.OpenFile(expectedPackagePath)));

            // Act - 2
            repository.RemovePackage(package);

            // Assert - 2
            Assert.Contains(expectedPackagePath, fileSystem.Deleted);
            Assert.Contains("MyPackage.1.0.0-beta2", fileSystem.Deleted);
        }

        [Fact]
        public void PackageLookupOperationsAreDeferredToUnderlyingExpandedPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("MyPackage", "1.0.0-beta2", "MyPackage.1.0.0-beta2.nupkg"));
            fileSystem.AddFile(Path.Combine("MyPackage", "1.0.0-beta2", "MyPackage.1.0.0-beta2.nupkg.sha512"));
            fileSystem.AddFile(Path.Combine("MyPackage", "1.0.0-beta2", "MyPackage.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>MyPackage</id><version>1.0.0-beta2</version><authors>None</authors><description>None</description></metadata></package>");
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act
            var getPackagesResult = repository.GetPackages();
            var findPackagesByIdResult = repository.FindPackagesById("MyPackage");
            var findPackageResult = repository.FindPackage("MyPackage", new SemanticVersion("1.0.0-beta2"));

            // Assert
            var actual = Assert.Single(getPackagesResult);
            Assert.Equal("MyPackage", actual.Id);
            Assert.Equal(new SemanticVersion("1.0.0-beta2"), actual.Version);

            actual = Assert.Single(findPackagesByIdResult);
            Assert.Equal("MyPackage", actual.Id);
            Assert.Equal(new SemanticVersion("1.0.0-beta2"), actual.Version);

            Assert.Equal("MyPackage", findPackageResult.Id);
            Assert.Equal(new SemanticVersion("1.0.0-beta2"), findPackageResult.Version);
        }

        [Fact]
        public void PackageAddAndRemovalOperationsAreDeferredToUnderlyingExpandedPackageRepository()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.1.0.0.nupkg"));
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"));

            var package = GetPackage();
            var repository = new LazyLocalPackageRepository(fileSystem);

            // Act - 1
            repository.AddPackage(package);

            // Assert - 1
            var reader = Manifest.ReadFrom(fileSystem.OpenFile(@"MyPackage\1.0.0-beta2\MyPackage.nuspec"), validateSchema: true);
            Assert.Equal("MyPackage", reader.Metadata.Id);
            Assert.Equal("1.0.0-beta2", reader.Metadata.Version);
            Assert.True(package.GetStream().ContentEquals(
                fileSystem.OpenFile(Path.Combine("MyPackage", "1.0.0-beta2", "MyPackage.1.0.0-beta2.nupkg"))));

            // Act - 2
            repository.RemovePackage(package);

            // Assert - 2
            var deleted = Assert.Single(fileSystem.Deleted);
            Assert.Equal(Path.Combine("MyPackage", "1.0.0-beta2"), deleted);
        }

        private static IPackage GetPackage()
        {
            var packageBuilder = new PackageBuilder
            {
                Id = "MyPackage",
                Version = new SemanticVersion("1.0.0-beta2"),
                Description = "Test description",
            };
            packageBuilder.Authors.Add("test");
            packageBuilder.Files.Add(
                PackageUtility.CreateMockedPackageFile(@"lib\net40", "MyPackage.dll", "lib contents").Object);

            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return new ZipPackage(memoryStream.ToStreamFactory(), enableCaching: false);
        }
    }
}
