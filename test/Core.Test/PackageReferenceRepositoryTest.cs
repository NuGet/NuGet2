using System;
using System.IO;
using System.Linq;
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage(package);

            // Assert
            Assert.Equal(@"C:\MockFileSystem\packages.config", path);
            Assert.True(fileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.AddPackage(package);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
            var A10 = PackageUtility.CreatePackage("A");
            var A20 = PackageUtility.CreatePackage("A", "2.0");

            // Act
            referenceRepository.RemovePackage(A10);
            referenceRepository.AddPackage(A20);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""2.0"" allowedVersions=""[1.0, 5.0)"" />
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
            var package = PackageUtility.CreatePackage("A");

            // Act
            referenceRepository.RemovePackage(package);

            // Assert
            Assert.True(fileSystem.FileExists("packages.config"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, sharedRepository.Object);
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(1, packages.Count);
            Assert.Same(packageA, packages[0]);
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


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
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


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
  <package id=""G"" version="""" />
  <package />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


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
  <package id=""G"" version="""" />
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
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


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
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);

            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.Equal(0, packages.Count);
        }
    }
}
