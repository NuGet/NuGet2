using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class SharedPackageRepositoryTest
    {
        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nuspec", "A.2.0\\A.2.0.nupkg")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nuspec", "B.1.0.0-alpha\\B.1.0.0-alpha.nupkg")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nuspec", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nupkg")]
        public void CallAddPackageWillAddBothNuspecFileAndNupkgFile(string id, string version, string nuspecPath, string nupkgPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.AddPackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.True(fileSystem.FileExists(nuspecPath));
            Assert.True(fileSystem.FileExists(nupkgPath));
        }

        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nuspec")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nuspec")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nuspec")]
        public void CallRemovePackageWillRemoveNuspecFile(string id, string version, string unexpectedPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.AddFile(unexpectedPath);
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.False(fileSystem.FileExists(unexpectedPath));
        }

        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nupkg")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nupkg")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nupkg")]
        public void CallRemovePackageWillRemoveNupkgFile(string id, string version, string unexpectedPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.AddFile(unexpectedPath);
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.False(fileSystem.FileExists(unexpectedPath));
        }

        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nuspec", "A.2.0\\A.2.0.nupkg")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nuspec", "B.1.0.0-alpha\\B.1.0.0-alpha.nupkg")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nuspec", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nupkg")]
        public void CallRemovePackageWillRemoveBothNupkgFileAndNuSpecFile(string id, string version, string nuspecPath, string nupkgPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.AddFile(nuspecPath);
            fileSystem.AddFile(nupkgPath);
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.False(fileSystem.FileExists(nuspecPath));
            Assert.False(fileSystem.FileExists(nupkgPath));
        }

        [Theory]
        [InlineData("A", "2.0.0.0", "A.2.0")]
        [InlineData("B", "1.0.0.0-alpha", "B.1.0.0-alpha")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm")]
        [InlineData("D", "4.0", "D.4.0.0.0")]
        [InlineData("E", "5.1.4", "E.5.1.4.0")]
        public void ExistChecksForPresenceOfPackageFileUnderDirectory(string id, string version, string path)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.CreateDirectory(path);
            fileSystem.AddFile(path + "\\" + path + ".nupkg");

            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            bool exists = repository.Exists(id, new SemanticVersion(version));

            // Assert
            Assert.True(exists);
        }

        [Theory]
        [InlineData("A", "2.0.0.0", "A.2.0")]
        [InlineData("B", "1.0.0.0-alpha", "B.1.0.0-alpha")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm")]
        [InlineData("D", "4.0", "D.4.0.0.0")]
        [InlineData("E", "5.1.4", "E.5.1.4.0")]
        public void ExistChecksForPresenceOfManifestFileUnderDirectory(string id, string version, string path)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.CreateDirectory(path);
            fileSystem.AddFile(path + "\\" + path + ".nuspec");

            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            bool exists = repository.Exists(id, new SemanticVersion(version));

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void FindPackageReturnUnzippedPackageObject()
        {
            // Arrange
            string manifestContent = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <version>1.0-alpha</version>
    <authors>dotnetjunky</authors>
    <owners />
    <id>One</id>
    <title />
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>My package description.</description>
  </metadata>
  <files />
</package>";

            var fileSystem = new MockFileSystem("x:\root");
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nuspec", manifestContent.AsStream());
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            IPackage package = repository.FindPackage("one", new SemanticVersion("1.0.0-alpha"));

            // Assert
            Assert.True(package is UnzippedPackage);
            Assert.Equal("One", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0-alpha"), package.Version);
            Assert.Equal(new string[] { "dotnetjunky" }, package.Authors);
        }

        [Fact]
        public void CallAddPackageWillNotCreatePackageConfigEntryToPackageConfig()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.AddPackage(PackageUtility.CreatePackage("A", "2.0"));

            // Assert
            Assert.False(configFileSystem.FileExists("packages.config"));
        }

        [Fact]
        public void CallAddPackageWillNotAddEntryToPackageConfigWhenPackageConfigAlreadyExists()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
</packages>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            // Assert
            Assert.True(configFileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
</packages>", configFileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void CallAddPackageReferenceEntryWillAddEntryToPackageConfigWhenPackageConfigAlreadyExists()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
</packages>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.AddPackageReferenceEntry("B", new SemanticVersion("1.0"));

            // Assert
            Assert.True(configFileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
</packages>", configFileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void CallAddPackageWillNotAddEntryToPackageConfigIfThatEntryAlreadyExists()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
</packages>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.AddPackage(PackageUtility.CreatePackage("B", "1.0"));

            // Assert
            Assert.True(configFileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
</packages>", configFileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void AddPackageDoesNotAddReferencesToSolutionLevelPackagesToSolutionConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel", tools: new[] { "Install.ps1" });

            // Act
            repository.AddPackage(solutionPackage);

            // Assert
            Assert.False(configFileSystem.FileExists("packages.config"));
        }

        [Fact]
        public void AddPackageReferenceEntryAddsReferenceToPackagesConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel", tools: new[] { "Install.ps1" });

            // Act
            repository.AddPackageReferenceEntry(solutionPackage.Id, solutionPackage.Version);
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""SolutionLevel"" version=""1.0"" />
</packages>", configFileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void RemovingAllSolutionLevelPackageDeletesConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel", tools: new[] { "Install.ps1" });
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""SolutionLevel"" version=""1.0"" />
</packages>");

            // Act
            repository.RemovePackage(solutionPackage);

            // Assert
            Assert.True(configFileSystem.Deleted.Contains("packages.config"));
        }

        [Fact]
        public void CallRemovePackageWillRemoveEntryFromPackageConfigWhenPackageConfigAlreadyExists()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
</packages>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage("B", "1.0"));

            // Assert
            Assert.True(configFileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
</packages>", configFileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void CallRemovePackageWillDeletePackageConfigWhenThereIsNoMoreEntry()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var configFileSystem = new MockFileSystem();
            configFileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" />
</packages>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage("A", "2.0"));

            // Assert
            Assert.False(configFileSystem.FileExists("packages.config"));
        }

        [Fact]
        public void RegisterRepositoryAddsRelativePathToRepositoriesConfig()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, new MockFileSystem());

            // Act
            repository.RegisterRepository(@"x:\foo\packages\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""packages\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void RegisterRepositoryDoesNotAddRelativePathToRepositoriesConfigIfExists()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Object.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>");
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, new MockFileSystem());

            // Act
            repository.RegisterRepository(@"x:\foo\A\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void GetRepositoryPathsRemovesInvalidOrNonExistantPathsAndReturnsRelativePaths()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.FileExists(@"A\packages.config")).Returns(true);
            fileSystem.Setup(m => m.FileExists(@"x:\foo\packages.config")).Returns(true);
            fileSystem.Setup(m => m.FileExists(@"..\..\packages.config")).Returns(true);
            fileSystem.Object.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
  <repository path=""x:\foo\packages.config"" />
  <repository path=""..\..\packages.config"" />
  <repository path="""" />
  <repository />
</repositories>");
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\bar\baz\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, new MockFileSystem());

            // Act
            var paths = repository.GetRepositoryPaths().ToList();

            // Assert
            Assert.Equal(2, paths.Count);
            Assert.Equal(@"A\packages.config", paths[0]);
            Assert.Equal(@"..\..\packages.config", paths[1]);
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""x:\foo\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void RepositoryPathsAreSavedInAlphabeticalOrder()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, new MockFileSystem());

            // Act
            repository.RegisterRepository(@"x:\foo\z\packages\packages.config");
            repository.RegisterRepository(@"x:\foo\X\packages\packages.config");
            repository.RegisterRepository(@"x:\foo\a\packages\packages.config");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""a\packages\packages.config"" />
  <repository path=""X\packages\packages.config"" />
  <repository path=""z\packages\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [Fact]
        public void IsReferencedReturnsTrueIfAnyOtherRepositoryReferencesAPackage()
        {
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

            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\bar\baz");
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExistsMethodChecksForPackageFileExistsAsOptimization(bool exists)
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.FileExists("A.1.0.0\\A.1.0.0.nupkg")).Returns(exists);
            
            var repository = new Mock<MockSharedRepository>(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object) { CallBase = true };

            // Act && Assert
            Assert.Equal(exists, repository.Object.Exists("A", new SemanticVersion("1.0.0")));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExistsMethodChecksForManifestFileExistsAsOptimization(bool exists)
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.FileExists("A.1.0.0\\A.1.0.0.nupkg")).Returns(exists);

            var repository = new Mock<MockSharedRepository>(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object) { CallBase = true };

            // Act && Assert
            Assert.Equal(exists, repository.Object.Exists("A", new SemanticVersion("1.0.0")));
        }

        public class MockSharedRepository : SharedPackageRepository
        {
            public MockSharedRepository(IPackagePathResolver resolver, IFileSystem fileSystem)
                : base(resolver, fileSystem, new MockFileSystem())
            {
            }

            protected override IPackageRepository CreateRepository(string path)
            {
                return Create(path);
            }

            public virtual IPackageRepository Create(string path)
            {
                return null;
            }
        }
    }
}
