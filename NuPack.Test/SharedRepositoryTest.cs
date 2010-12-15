using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class SharedRepositoryTest {
        [TestMethod]
        public void RegisterRepositoryAddsRelativePathToRepositoriesConfig() {
            // Arrange
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            fileSystem.Setup(m => m.Root).Returns(@"c:\foo\");
            var repository = new SharedPackageRepository(new DefaultPackagePathResolver(fileSystem.Object), fileSystem.Object);

            // Act
            repository.RegisterRepository(@"c:\foo\packages\packages.config");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""packages\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [TestMethod]
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
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""B\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [TestMethod]
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
            Assert.AreEqual(2, paths.Count);
            Assert.AreEqual(@"A\packages.config", paths[0]);
            Assert.AreEqual(@"..\..\packages.config", paths[1]);
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<repositories>
  <repository path=""A\packages.config"" />
  <repository path=""c:\foo\packages.config"" />
</repositories>", fileSystem.Object.ReadAllText("repositories.config"));
        }

        [TestMethod]
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
            Assert.IsTrue(repository.Object.IsReferenced("A", new Version("1.0")));
            Assert.IsTrue(repository.Object.IsReferenced("B", new Version("1.0")));
            Assert.IsFalse(repository.Object.IsReferenced("C", new Version("1.0")));
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
