using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class PackageReferenceRepositoryTest {
        [TestMethod]
        public void RegisterIfNecessaryDoesNotRegistersWithSharedRepositoryIfRepositoryDoesNotContainsPackages() {
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
            Assert.IsNull(path);
        }

        [TestMethod]
        public void RegisterIfNecessaryRegistersWithSharedRepositoryIfRepositoryContainsPackages() {
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
            Assert.AreEqual(@"C:\MockFileSystem\packages.config", path);
        }

        [TestMethod]
        public void AddPackageAddsEntryToPackagesConfig() {
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
            Assert.AreEqual(@"C:\MockFileSystem\packages.config", path);
            Assert.IsTrue(fileSystem.FileExists("packages.config"));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [TestMethod]
        public void AddPackageDoesNotAddEntryToPackagesConfigIfExists() {
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
            Assert.IsTrue(fileSystem.FileExists("packages.config"));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [TestMethod]
        public void RemovePackageRemovesEntryFromPackagesConfig() {
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
            Assert.IsTrue(fileSystem.FileExists("packages.config"));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""B"" version=""1.0"" />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [TestMethod]
        public void RemovePackageRemovesEntryFromPackagesConfigDeletesFileAndUnregistersRepositoryIfLastEntry() {
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
            Assert.IsFalse(fileSystem.FileExists("packages.config"));
            Assert.IsNotNull(path);
            Assert.AreEqual(@"C:\MockFileSystem\packages.config", path);
        }

        
        [TestMethod]
        public void GetPackagesReturnsPackagesFromSourceRepositoryListedInPackagesConfig() {
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
            Assert.AreEqual(1, packages.Count);
            Assert.AreSame(packageA, packages[0]);
        }

        [TestMethod]
        public void GetPackagesReturnsOnlyValidPackagesFromSourceRepositoryListedInPackagesConfig() {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""1.0"" />
  <package id="""" version=""1.0"" />
  <package id=""G"" version="""" />
  <package />
</packages>");
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);


            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.AreSame(packageA, packages[0]);
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""A"" version=""1.0"" />
  <package id=""B"" version=""1.0"" />
  <package id="""" version=""1.0"" />
  <package id=""G"" version="""" />
  <package />
</packages>", fileSystem.ReadAllText("packages.config"));
        }

        [TestMethod]
        public void GetPackagesWithMalformedPackagesConfigThrows() {
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

        [TestMethod]
        public void GetPackagesNoPackagesConfigReturnsEmptyList() {
            // Arrange
            var repository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>();
            var packageA = PackageUtility.CreatePackage("A");
            repository.Object.AddPackage(packageA);
            var fileSystem = new MockFileSystem();
            var referenceRepository = new PackageReferenceRepository(fileSystem, repository.Object);

            // Act
            var packages = referenceRepository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }
    }
}
