using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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
        public void CallAddPackageWillAddNupkgFileButNoNuspecFile(string id, string version, string nuspecPath, string nupkgPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            
            // Act            
            repository.AddPackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.False(fileSystem.FileExists(nuspecPath));
            Assert.True(fileSystem.FileExists(nupkgPath));
        }

        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nuspec", "A.2.0\\A.2.0.nupkg")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nuspec", "B.1.0.0-alpha\\B.1.0.0-alpha.nupkg")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nuspec", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nupkg")]
        public void CallAddPackageToAddBothNuspecFileAndNupkgFile(string id, string version, string nuspecPath, string nupkgPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            repository.PackageSaveMode = PackageSaveModes.Nupkg | PackageSaveModes.Nuspec;

            // Act            
            repository.AddPackage(PackageUtility.CreatePackage(id, version));

            // Assert
            Assert.True(fileSystem.FileExists(nuspecPath));
            Assert.True(fileSystem.FileExists(nupkgPath));
        }

        [Fact]
        public void CallAddPackageWillAddNuspecWithReferencesPreserved()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            var package = PackageUtility.CreatePackage("A", "1.0", content: new[] { "A.txt", "scripts\\b.txt" });
            var mockedPackage = Mock.Get(package);
            mockedPackage.Setup(m => m.PackageAssemblyReferences).Returns(
                new PackageReferenceSet[] { 
                    new PackageReferenceSet(new FrameworkName(".NETFramework, Version=4.0"), new [] { "A.dll" }),
                    new PackageReferenceSet(null, new [] { "B.dll" }),
                });

            // Act
            repository.AddPackage(package);

            // Assert
            Assert.False(fileSystem.FileExists("A.1.0\\A.1.0.nuspec"));
            Assert.True(fileSystem.FileExists("A.1.0\\A.1.0.nupkg"));
        }

        [Theory]
        [InlineData("A", "2.0", "A.2.0\\A.2.0.nuspec")]
        [InlineData("B", "1.0.0-alpha", "B.1.0.0-alpha\\B.1.0.0-alpha.nuspec")]
        [InlineData("C", "3.1.2.4-rtm", "C.3.1.2.4-rtm\\C.3.1.2.4-rtm.nuspec")]
        public void CallRemovePackageWillRemoveNuspecFile(string id, string version, string unexpectedPath)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
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
            var fileSystem = new MockFileSystem("x:\\root");
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
            var fileSystem = new MockFileSystem("x:\\root");
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

        [Fact]
        public void CallRemovePackageWillRemoveTheWholePackageDirectory()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
            fileSystem.AddFile("A.2.0\\A.2.0.nupkg");
            fileSystem.AddFile("A.2.0\\A.2.0.nuspec");
            fileSystem.AddFile("A.2.0\\random");
            fileSystem.AddFile("A.2.0\\content\\file.txt");
            fileSystem.AddFile("A.2.0\\readme.txt");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage("A", "2.0"));

            // Assert
            Assert.False(fileSystem.FileExists("A.2.0\\A.2.0.nupkg"));
            Assert.False(fileSystem.FileExists("A.2.0\\A.2.0.nuspec"));
            Assert.False(fileSystem.DirectoryExists("A.2.0"));
        }

        [Fact]
        public void CallRemovePackageWillDeleteNuspecAndNupkgFileBeforeDeletingTheWholePackageDirectory()
        {
            // Arrange
            var fileSystem = new MockFileSystemWithDeleteVerification();
            fileSystem.AddFile("A.2.0\\A.2.0.nupkg");
            fileSystem.AddFile("A.2.0\\A.2.0.nuspec");
            fileSystem.AddFile("A.2.0\\random");
            fileSystem.AddFile("A.2.0\\content\\file.txt");
            fileSystem.AddFile("A.2.0\\readme.txt");
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            repository.RemovePackage(PackageUtility.CreatePackage("A", "2.0"));

            // Assert
            Assert.False(fileSystem.FileExists("A.2.0\\A.2.0.nupkg"));
            Assert.False(fileSystem.FileExists("A.2.0\\A.2.0.nuspec"));
            Assert.False(fileSystem.DirectoryExists("A.2.0"));

            Assert.True(fileSystem.IsFileDeleted("A.2.0\\A.2.0.nupkg"));
            Assert.True(fileSystem.IsFileDeleted("A.2.0\\A.2.0.nuspec"));
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
            var fileSystem = new MockFileSystem("x:\\root");
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
        public void ExistDoesNotCheckForPresenceOfManifestFileUnderDirectory(string id, string version, string path)
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\root");
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
        public void FindPackageReturnOptimizedZipPackageObject()
        {
            // Arrange
            var packageStream = PackageUtility.CreateSimplePackageStream("one", "1.0.0-alpha");

            var fileSystem = new MockFileSystem("x:\\root");
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nupkg", packageStream);
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nuspace", "rubbish".AsStream());

            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            IPackage package = repository.FindPackage("one", new SemanticVersion("1.0.0-alpha"));

            // Assert
            Assert.True(package is OptimizedZipPackage);
            Assert.Equal("one", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0-alpha"), package.Version);
            Assert.Equal("Test description", package.Description);
        }

        [Fact]
        public void GetPackagesDoesNotReturnDuplicatedPackagesIfBothNuspecAndNupkgFilesArePresent()
        {
            // Arrange
            var packageStream = PackageUtility.CreateSimplePackageStream("one", "1.0.0-alpha");

            var fileSystem = new MockFileSystem("x:\\root");
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nupkg", packageStream);
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nuspec", "rubbish".AsStream());

            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.True(packages[0] is OptimizedZipPackage);
            Assert.Equal("one", packages[0].Id);
            Assert.Equal(new SemanticVersion("1.0.0-alpha"), packages[0].Version);
        }

        [Fact]
        public void FindPackagesByIdAlwaysReturnsOptimizedZipPackageEvenIfNuspecIfPresent()
        {
            // Arrange
            string manifestContent = @"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>One</id>
    <version>1.0.0-alpha</version>
    <authors>test</authors>
    <description>My package description.</description>
  </metadata>
  <files />
</package>";

            var fileSystem = new MockFileSystem("x:\\root");
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nuspec", manifestContent.AsStream());

            var packageStream = PackageUtility.CreateSimplePackageStream("One", "1.0.0-alpha");
            fileSystem.AddFile("one.1.0.0-alpha\\one.1.0.0-alpha.nupkg", packageStream);

            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);

            // Act
            var packages = repository.FindPackagesById("one").ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            var package = packages[0];
            Assert.True(package is OptimizedZipPackage);
            Assert.Equal("One", package.Id);
            Assert.Equal(new SemanticVersion("1.0.0-alpha"), package.Version);
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
            repository.AddPackage(PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new [] {"lib\\this.dll"}));

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
            repository.AddPackage(PackageUtility.CreatePackage("B", "1.0", content: new [] { "whack.txt" }));

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
        public void AddPackageAddReferencesToSolutionLevelPackagesToSolutionConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel", content: Enumerable.Empty<string>(), tools: new[] { "Install.ps1" });

            // Act
            repository.AddPackage(solutionPackage);

            // Assert
            Assert.True(configFileSystem.FileExists("packages.config"));
        }

        // Tests that adding a solution level package which depends on an already installed solution 
        // level package will succeed.
        [Fact]
        public void AddPackageAddSolutionLevelPackageDependingOnAnotherSolutionLevelPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage1 = PackageUtility.CreatePackage("SolutionLevel1", content: Enumerable.Empty<string>(), tools: new[] { "Install.ps1" });
            var solutionPackage2 = PackageUtility.CreatePackage("SolutionLevel2", content: Enumerable.Empty<string>(), tools: new[] { "Install.ps1" }, dependencies: new[] { new PackageDependency("SolutionLevel1")});            
            // Act
            repository.AddPackage(solutionPackage1);
            repository.AddPackage(solutionPackage2);

            // Assert
            var packageReferences = repository.PackageReferenceFile.GetPackageReferences()
                .Select(p => p.Id).OrderBy(id => id).ToArray();
            Assert.Equal(new [] { "SolutionLevel1", "SolutionLevel2" }, packageReferences);
        }

        // Tests that adding a solution level package which depends on a project level package will
        // not succeed.
        [Fact]
        public void AddPackageAddSolutionLevelPackageDependingOnProjectLevelPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel2", tools: new[] { "Install.ps1" }, dependencies: new[] { new PackageDependency("ProjectLevel1") });

            // Act
            repository.AddPackage(solutionPackage);

            // Assert
            var packageReferences = repository.PackageReferenceFile.GetPackageReferences().ToArray();
            Assert.True(packageReferences.IsEmpty());
        }

        [Fact]
        public void AddPackageDoesNotAddReferencesToMetadataPackagesToSolutionConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("MetadataPackage", dependencies: new [] { new PackageDependency("A") }, tools: new[] { "Install.ps1" });

            // Act
            repository.AddPackage(solutionPackage);

            // Assert
            Assert.False(configFileSystem.FileExists("packages.config"));
        }

        [Fact]
        public void AddPackageDoesNotAddEntryToSolutionConfigFileForProjectLevelPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var configFileSystem = new MockFileSystem();
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, configFileSystem);
            var solutionPackage = PackageUtility.CreatePackage("SolutionLevel", content: new[] { "file.txt" });

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
            Assert.True(configFileSystem.Deleted.Contains(Path.Combine(configFileSystem.Root, "packages.config")));
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
            repository.RegisterRepository(new PackageReferenceFile(new MockFileSystem(), @"x:\foo\packages\packages.config"));

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
            var fileSystem = new MockFileSystem(@"x:\foo");
            fileSystem.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, new MockFileSystem());
            var packageReferenceFile = new PackageReferenceFile(new MockFileSystem(), @"x:\foo\A\packages.config");

            // Act
            repository.RegisterRepository(packageReferenceFile);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>", fileSystem.ReadAllText("repositories.config"));
        }

        [Fact]
        public void GetRepositoryPathsRemovesInvalidOrNonExistantPathsAndReturnsRelativePaths()
        {
            // Arrange
            var fileSystem = new MockFileSystem(@"x:\foo\bar\baz");
            fileSystem.AddFile(@"A\packages.config", "");
            fileSystem.AddFile(@"x:\foo\packages.config", "");
            fileSystem.AddFile(@"..\..\packages.config", "");
            fileSystem.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
  <repository path=""x:\foo\packages.config"" />
  <repository path=""..\..\packages.config"" />
  <repository path="""" />
  <repository />
</repositories>");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem, new MockFileSystem());

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
</repositories>", fileSystem.ReadAllText("repositories.config"));
        }

        [Fact]
        public void RepositoryPathsAreSavedInAlphabeticalOrder()
        {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"x:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object, new MockFileSystem());

            // Act
            repository.RegisterRepository(
                new PackageReferenceFile(new MockFileSystem(), @"x:\foo\z\packages\packages.config"));
            repository.RegisterRepository(
                new PackageReferenceFile(new MockFileSystem(), @"x:\foo\X\packages\packages.config"));
            repository.RegisterRepository(
                new PackageReferenceFile(new MockFileSystem(), @"x:\foo\a\packages\packages.config"));

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
            var fileSystem = new MockFileSystem(@"x:\foo\bar\baz");
            fileSystem.AddFile(@"A\packages.config", "");
            fileSystem.AddFile(@"..\..\packages.config", "");
            fileSystem.AddFile("repositories.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""..\..\packages.config"" />
  <repository />
</repositories>");

            var repository = new Mock<MockSharedRepository>(new DefaultPackagePathResolver(fileSystem), fileSystem) { CallBase = true };
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

        [Fact]
        public void RegisterRepositoryUseTheStoreFileSystem()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            var storeFileSystem = new MockFileSystem("y:\\");
            var configFileSystem = new MockFileSystem("z:\\");
            var resolver = new DefaultPackagePathResolver(fileSystem);

            var repository = new SharedPackageRepository(resolver, fileSystem, storeFileSystem, configFileSystem);

            // Act
            repository.RegisterRepository(new PackageReferenceFile(new MockFileSystem(), "x:\\project1\\path"));

            // Assert
            Assert.True(storeFileSystem.FileExists("repositories.config"));
            string content = storeFileSystem.ReadAllText("repositories.config");
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""project1\path"" />
</repositories>", content);
        }

        [Fact]
        public void UnregisterRepositoryUseTheStoreFileSystem()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            var storeFileSystem = new MockFileSystem("y:\\");
            storeFileSystem.AddFile("repositories.config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""project1\path"" />
  <repository path=""..\one\two"" />
</repositories>");
            var configFileSystem = new MockFileSystem("z:\\");
            var resolver = new DefaultPackagePathResolver(fileSystem);

            var repository = new SharedPackageRepository(resolver, fileSystem, storeFileSystem, configFileSystem);

            // Act
            repository.UnregisterRepository(new PackageReferenceFile(new MockFileSystem(), "x:\\project1\\path"));

            // Assert
            Assert.True(storeFileSystem.FileExists("repositories.config"));
            string content = storeFileSystem.ReadAllText("repositories.config");
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""..\one\two"" />
</repositories>", content);
        }

        [Fact]
        public void UnregisterRepositoryUseTheStoreFileSystemAndDeleteRepositoryConfig()
        {
            // Arrange
            var fileSystem = new MockFileSystem("x:\\");
            var storeFileSystem = new MockFileSystem("y:\\");
            storeFileSystem.AddFile("repositories.config",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""project1\path"" />
</repositories>");
            var configFileSystem = new MockFileSystem("z:\\");
            var resolver = new DefaultPackagePathResolver(fileSystem);

            var repository = new SharedPackageRepository(resolver, fileSystem, storeFileSystem, configFileSystem);

            // Act
            repository.UnregisterRepository(new PackageReferenceFile(new MockFileSystem(), "x:\\project1\\path"));

            // Assert
            Assert.False(storeFileSystem.FileExists("repositories.config"));
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

        private class MockFileSystemWithDeleteVerification : MockFileSystem
        {
            private readonly HashSet<string> _deletedFiles = new HashSet<string>();

            public override void DeleteFile(string path)
            {
                _deletedFiles.Add(path);
                base.DeleteFile(path);
            }

            public bool IsFileDeleted(string path)
            {
                return _deletedFiles.Contains(path);
            }
        }
    }
}
