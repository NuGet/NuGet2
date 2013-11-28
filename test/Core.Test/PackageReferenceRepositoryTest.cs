using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class PackageReferenceRepositoryTest
    {
        [Fact]
        public void RegisterIfNecessaryDoesNotRegistersWithSharedRepositoryIfRepositoryDoesNotContainsPackages()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.RegisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();

            // Act
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            referenceRepository.RegisterIfNecessary();

            // Assert
            Assert.Null(path);
        }

        [Fact]
        public void RegisterIfNecessaryRegistersWithSharedRepositoryIfRepositoryContainsPackages()
        {
            // Arrange
            var sharedRepository = new Mock<MockPackageRepository>().As<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.RegisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();
            IPackage package = PackageUtility.CreatePackage("A");
            sharedRepository.Object.AddPackage(package);

            // Act
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            referenceRepository.AddPackage(package);
            referenceRepository.RegisterIfNecessary();

            // Assert
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
        }

        [Fact]
        public void AddPackageAddsEntryToPackagesConfig()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.RegisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage(package);

            // Assert
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void AddPackageAddsEntryToPackagesConfigWithTargetFramework()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.RegisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            //var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage("A", new SemanticVersion("1.0"), false, new FrameworkName("Silverlight, Version=2.0"));

            // Assert
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""sl20"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void AddPackageAddsEntryToPackagesConfigWithDevelopmentDependency()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.RegisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            //var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage("A", new SemanticVersion("1.0"), true, new FrameworkName("Silverlight, Version=2.0"));

            // Assert
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""sl20"" developmentDependency=""true""/>
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void AddPackageDoesNotAddEntryToPackagesConfigIfExists()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage(package);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void RemovingAndAddingPackageReferenceWithSameIdPreservesConstraint()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" allowedVersions=""[1.0, 5.0)"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var A10 = PackageUtility.CreatePackage("A");
            var A20 = PackageUtility.CreatePackage("A", "2.0");

            // Act
            referenceRepository.RemovePackage(A10);
            referenceRepository.AddPackage(A20);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" allowedVersions=""[1.0, 5.0)"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void RemovingAndAddingPackageReferenceWithSameIdPreservesDevelopmentFlag()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" developmentDependency=""false"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var A10 = PackageUtility.CreatePackage("A");
            var A20 = PackageUtility.CreatePackage("A", "2.0");

            // Act
            referenceRepository.RemovePackage(A10);
            referenceRepository.AddPackage(A20);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" developmentDependency=""false"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void RemovePackageRemovesEntryFromPackagesConfig()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.RemovePackage(package);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""B"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void RemovePackageRemovesEntryFromPackagesConfigDeletesFileAndUnregistersRepositoryIfLastEntry()
        {
            // Arrange
            var sharedRepository = new Mock<ISharedPackageRepository>();
            string path = null;
            sharedRepository.Setup(m => m.UnregisterRepository(It.IsAny<string>()))
                            .Callback<string>(p => path = p);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.RemovePackage(package);

            // Assert
            Assert.False(fileSystem.FileExists("packages.config"));
            Assert.NotNull(path);
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
        }

        [Fact]
        public void GetPackagesReturnsPackagesFromSourceRepositoryListedInPackagesConfig()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Same(packageA, packages[0]);
        }

        [Fact]
        public void GetPackagesLoadsFromProjectPackagesConfigIfPresent()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Same(packageA, packages[0]);

            Assert.True(fileSystem.FileExists("packages.cool.config"));
        }

        [Fact]
        public void GetPackagesFavorsProjectPackagesConfigOverPlainPackagesConfig()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");

            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Same(packageA, packages[0]);

            Assert.True(fileSystem.FileExists("packages.cool.config"));
        }

        [Fact]
        public void RemovePackageDeleteProjectConfigFileIfNoPackageLeft()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""3.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);

            var packageB = PackageUtility.CreatePackage("B", "3.0");

            // Act
            referenceRepository.RemovePackage(packageA);
            referenceRepository.RemovePackage(packageB);

            // Assert
            Assert.False(fileSystem.FileExists("packages.config"));
            Assert.False(fileSystem.FileExists("packages.cool.config"));
        }

        [Fact]
        public void GetPackagesLoadsPlainPackagesConfigIfProjectPackagesConfigDoesNotExist()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");

            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""B"" version=""1.0"" />
  <package id=""C"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Same(packageA, packages[0]);

            Assert.True(fileSystem.FileExists("packages.cool.config"));
        }

        [Fact]
        public void GetPackageTargetFrameworkReturnsCorrectValue()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" targetFramework=""winrt45"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            var targetFramework = referenceRepository.GetPackageTargetFramework("A");

            // Assert
            Assert.Equal(new FrameworkName(".NETCore, Version=4.5"), targetFramework);
        }

        [Fact]
        public void GetPackageTargetFrameworkReturnsNullIfTargetFrameworkAttributeIsNotPresent()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            var targetFramework = referenceRepository.GetPackageTargetFramework("A");

            // Assert
            Assert.Null(targetFramework);
        }

        [Fact]
        public void GetPackageTargetFrameworkReturnsNullIfPackageIdIsNotPresent()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            var targetFramework = referenceRepository.GetPackageTargetFramework("B");

            // Assert
            Assert.Null(targetFramework);
        }


        [Fact]
        public void GetConstraintReturnsConstraintListedForPackageIdInPackagesConfig()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" allowedVersions=""[1.0, 3.0)"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);


            // Act
            IVersionSpec constraint = referenceRepository.GetConstraint("A");

            // Assert
            Assert.NotNull(constraint);
            Assert.True(constraint.IsMinInclusive);
            Assert.False(constraint.IsMaxInclusive);
            Assert.Equal(new SemanticVersion("1.0"), constraint.MinVersion);
            Assert.Equal(new SemanticVersion("3.0"), constraint.MaxVersion);
        }

        [Fact]
        public void GetConstraintThrowsIfConstrainInvalid()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" allowedVersions=""[-1.3, 3)"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);


            // Act & Assert
            ExceptionAssert.Throws<InvalidDataException>(() => referenceRepository.GetConstraint("A"), "Unable to parse version value '[-1.3, 3)' from 'packages.config'.");
        }

        [Fact]
        public void GetPackagesReturnsOnlyValidPackagesFromSourceRepositoryListedInPackagesConfig()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var packageC = PackageUtility.CreatePackage("C", "2.0");
            repository.Object.AddPackage(packageC);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""C"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
  <package id=""A"" version=""1.0"" />
  <package id="""" version=""1.0"" />
  <package />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Same(packageC, packages[0]);
            Assert.Same(packageA, packages[1]);
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""C"" version=""2.0"" />
  <package id=""B"" version=""1.0"" />
  <package id=""A"" version=""1.0"" />
  <package id="""" version=""1.0"" />
  <package />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [Fact]
        public void GetPackagesWithMalformedPackagesConfigThrows()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package ");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);


            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => referenceRepository.GetPackages().ToList(), @"Error reading 'C:\MockFileSystem\packages.config'.");
        }

        [Fact]
        public void GetPackagesNoPackagesConfigReturnsEmptyList()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }

        [Fact]
        public void PackageReferenceRepositoryImplementsILatestPackageLookupInterfaceCorrectly()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("A", "1.5");
            var packageD = PackageUtility.CreatePackage("A", "2.0-beta");
            repository.Object.AddPackage(packageA);
            repository.Object.AddPackage(packageB);
            repository.Object.AddPackage(packageC);
            repository.Object.AddPackage(packageD);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""2.0"" />
  <package id=""A"" version=""1.5"" />
  <package id=""A"" version=""2.0-beta"" />
</packages>");

            ILatestPackageLookup referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            SemanticVersion latestVersion;
            bool result = referenceRepository.TryFindLatestPackageById("A", out latestVersion);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("2.0-beta"), latestVersion);
        }

        [Fact]
        public void PackageReferenceRepositoryFindTheOnlyPackageAsLatestPackage()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("A", "1.5");
            var packageD = PackageUtility.CreatePackage("A", "2.0-beta");
            repository.Object.AddPackage(packageA);
            repository.Object.AddPackage(packageB);
            repository.Object.AddPackage(packageC);
            repository.Object.AddPackage(packageD);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""2.0"" />
  <package id=""A"" version=""1.5"" />
  <package id=""A"" version=""2.0-beta"" />
</packages>");

            ILatestPackageLookup referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            SemanticVersion latestVersion;
            bool result = referenceRepository.TryFindLatestPackageById("B", out latestVersion);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("2.0"), latestVersion);
        }

        [Fact]
        public void PackageReferenceRepositoryDoNotFindLatestPackageIfItDoesNotExist()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("A", "1.5");
            var packageD = PackageUtility.CreatePackage("A", "2.0-beta");
            repository.Object.AddPackage(packageA);
            repository.Object.AddPackage(packageB);
            repository.Object.AddPackage(packageC);
            repository.Object.AddPackage(packageD);

            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""2.0"" />
  <package id=""A"" version=""1.5"" />
  <package id=""A"" version=""2.0-beta"" />
</packages>");

            ILatestPackageLookup referenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: repository.Object);

            // Act
            SemanticVersion latestVersion;
            bool result = referenceRepository.TryFindLatestPackageById("C", out latestVersion);

            // Assert
            Assert.False(result);
            Assert.Null(latestVersion);
        }

        [Fact]
        public void GetPackageReferencesFindLatestEntryCorrectly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <packages>
          <package id=""A"" version=""1.3.4"" />
          <package id=""A"" version=""2.5"" />
          <package id=""B"" version=""1.0"" />
          <package id=""C"" version=""2.1.4"" />
        </packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var sharedRepository = new Mock<MockPackageRepository>().As<ISharedPackageRepository>();
            var packageReferenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);

            // Act
            SemanticVersion version;
            bool result = packageReferenceRepository.TryFindLatestPackageById("A", out version);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("2.5"), version);
        }

        [Fact]
        public void GetPackageReferencesFindLatestPrereleaseEntryCorrectly()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <packages>
          <package id=""A"" version=""1.3.4"" />
          <package id=""A"" version=""2.5-beta"" />
          <package id=""B"" version=""1.0"" />
          <package id=""C"" version=""2.1.4"" />
        </packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var sharedRepository = new Mock<MockPackageRepository>().As<ISharedPackageRepository>();
            var packageReferenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);

            // Act
            SemanticVersion version;
            bool result = packageReferenceRepository.TryFindLatestPackageById("A", out version);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("2.5-beta"), version);
        }

        [Fact]
        public void GetPackageReferencesFindTheOnlyVersionAsLatestVersion()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <packages>
          <package id=""A"" version=""1.3.4"" />
          <package id=""A"" version=""2.5-beta"" />
          <package id=""B"" version=""1.0"" />
          <package id=""C"" version=""2.1.4"" />
        </packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var sharedRepository = new Mock<MockPackageRepository>().As<ISharedPackageRepository>();
            var packageReferenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);

            // Act
            SemanticVersion version;
            bool result = packageReferenceRepository.TryFindLatestPackageById("B", out version);

            // Assert
            Assert.True(result);
            Assert.Equal(new SemanticVersion("1.0"), version);
        }

        [Fact]
        public void GetPackageReferencesReturnsFalseForNonExistentId()
        {
            // Arrange
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <packages>
          <package id=""A"" version=""1.3.4"" />
          <package id=""A"" version=""2.5-beta"" />
          <package id=""B"" version=""1.0"" />
          <package id=""C"" version=""2.1.4"" />
        </packages>";
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", config);
            var sharedRepository = new Mock<MockPackageRepository>().As<ISharedPackageRepository>();
            var packageReferenceRepository = new PackageReferenceRepository(fileSystem, projectName: null, sourceRepository: sharedRepository.Object);

            // Act
            SemanticVersion version;
            bool result = packageReferenceRepository.TryFindLatestPackageById("does-not-exist", out version);

            // Assert
            Assert.False(result);
            Assert.Null(version);
        }

        [Fact]
        public void AddPackagePreservesProjectConfigFile()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);

            var packageB = PackageUtility.CreatePackage("B", "2.0-alpha");

            // Act
            referenceRepository.AddPackage(packageB);

            // Assert
            Assert.False(fileSystem.FileExists("packages.config"));
            Assert.True(fileSystem.FileExists("packages.cool.config"));

            string content = fileSystem.ReadAllText("packages.cool.config");

            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""2.0-alpha"" />
</packages>", content);
        }

        [Fact]
        public void RemovePackagePreservesProjectConfigFile()
        {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.cool.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""3.0"" />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, projectName: "cool", sourceRepository: repository.Object);

            var packageB = PackageUtility.CreatePackage("B", "3.0");

            // Act
            referenceRepository.RemovePackage(packageB);

            // Assert
            Assert.False(fileSystem.FileExists("packages.config"));
            Assert.True(fileSystem.FileExists("packages.cool.config"));

            string content = fileSystem.ReadAllText("packages.cool.config");

            AssertConfig(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>", content);
        }

        private static void AssertConfig(string expected, string actual)
        {
            Assert.Equal(expected.Where(c => !Char.IsWhiteSpace(c)), actual.Where(c => !Char.IsWhiteSpace(c)));

            // Verify the actual document is parse-able as an xml
            XDocument.Load(new StringReader(actual));
        }
    }
}