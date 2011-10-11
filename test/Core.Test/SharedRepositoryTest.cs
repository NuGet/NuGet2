using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test {
    
    public class SharedRepositoryTest {
        [Fact]
        public void RegisterRepositoryAddsRelativePathToRepositoriesConfig() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object);

            // Act
            repository.RegisterRepository(@"c:\foo\packages\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""packages\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void RegisterRepositoryDoesNotAddRelativePathToRepositoriesConfigIfExists() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Object.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>");
            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object);

            // Act
            repository.RegisterRepository(@"c:\foo\A\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void GetRepositoryPathsRemovesInvalidOrNonExistantPathsAndReturnsRelativePaths() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.FileExists(@"A\packages.config")).Returns(true);
            fileSystem.Setup(m => m.FileExists(@"c:\foo\packages.config")).Returns(true);
            fileSystem.Setup(m => m.FileExists(@"..\..\packages.config")).Returns(true);
            fileSystem.Object.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
  <repository path=""c:\foo\packages.config"" />
  <repository path=""..\..\packages.config"" />
  <repository path="""" />
  <repository />
</repositories>");
            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\bar\baz\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object);

            // Act
            var paths = repository.GetRepositoryPaths().ToList();

            // Assert
            Assert.Equal(2, paths.Count);
            Assert.Equal(@"A\packages.config", paths[0]);
            Assert.Equal(@"..\..\packages.config", paths[1]);
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""c:\foo\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void RepositoryPathsAreSavedInAlphabeticalOrder() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object);

            // Act
            repository.RegisterRepository(@"c:\foo\z\packages\packages.config");
            repository.RegisterRepository(@"c:\foo\X\packages\packages.config");
            repository.RegisterRepository(@"c:\foo\a\packages\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""a\packages\packages.config"" />
  <repository path=""X\packages\packages.config"" />
  <repository path=""z\packages\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void IsReferencedReturnsTrueIfAnyOtherRepositoryReferencesAPackage() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.FileExists(@"A\packages.config")).Returns(true);
            fileSystem.Setup(m => m.FileExists(@"..\..\packages.config")).Returns(true);
            fileSystem.Object.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""..\..\packages.config"" />
  <repository />
</repositories>");

            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\bar\baz");
            var repository = new Mock<MockSharedRepository>(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object) { CallBase = true };
            var r1 = new MockPackageRepository {
                PackageUtility.CreatePackage("A")
            };
            var r2 = new MockPackageRepository {
                PackageUtility.CreatePackage("B")
            };
            repository.Setup(m => m.Create(@"A\packages.config")).Returns(r1);
            repository.Setup(m => m.Create(@"..\..\packages.config")).Returns(r2);


            // Act && Assert
            Assert.True(repository.Object.IsReferenced("A", new SemanticVersion("1.0")));
            Assert.True(repository.Object.IsReferenced("B", new SemanticVersion("1.0")));
            Assert.False(repository.Object.IsReferenced("C", new SemanticVersion("1.0")));
        }

        public class MockSharedRepository : SharedPackageRepository {
            public MockSharedRepository(IPackagePathResolver resolver, IFileSystem fileSystem)
                : base(resolver, fileSystem) {
            }

            protected override IPackageRepository CreateRepository(string path) {
                return Create(path);
            }

            public virtual IPackageRepository Create(string path) {
                return null;
            }
        }
    }
}
