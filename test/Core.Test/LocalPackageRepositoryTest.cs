using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class LocalPackageRepositoryTest {
        [TestMethod]
        public void GetPackageFilesOnlyDetectsFilesWithPackageExtension() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile("foo.nupkg");
            mockFileSystem.AddFile("bar.zip");

            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);

            // Act
            var files = repository.GetPackageFiles().ToList();

            // Assert
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("foo.nupkg", files[0]);
        }

        [TestMethod]
        public void GetPackageFilesDetectsFilesInRootOrFirstLevelOfFolders() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile("P1.nupkg");
            mockFileSystem.AddFile("bar.zip");
            mockFileSystem.AddFile(@"baz\P2.nupkg");
            mockFileSystem.AddFile(@"A\B\P3.nupkg");
            mockFileSystem.AddFile(@"A\P4.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);

            // Act
            var files = repository.GetPackageFiles().ToList();

            // Assert
            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(@"baz\P2.nupkg", files[0]);
            Assert.AreEqual(@"A\P4.nupkg", files[1]);
            Assert.AreEqual("P1.nupkg", files[2]);
        }

        [TestMethod]
        public void GetPackagesOnlyRetrievesPackageFilesWhereLastModifiedIsOutOfDate() {
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
            Func<string, IPackage> openPackage = p => {
                results.Add(p);
                string id = Path.GetFileNameWithoutExtension(p);
                return PackageUtility.CreatePackage(id, "1.0");
            };

            // Populate cache
            lastModified["P1.nupkg"] = GetDateTimeOffset(seconds: 30);
            lastModified["P2.nupkg"] = GetDateTimeOffset(seconds: 30);
            repository.GetPackages(openPackage).ToList();

            // Verify that both packages have been created from the file system
            Assert.AreEqual(2, results.Count);
            results.Clear();

            // Act
            lastModified["P1.nupkg"] = GetDateTimeOffset(seconds: 35);
            lastModified["P2.nupkg"] = GetDateTimeOffset(seconds: 30);
            repository.GetPackages(openPackage).ToList();

            // Assert
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0], "P1.nupkg");
        }

        [TestMethod]
        public void AddPackageAddsFileToFileSystem() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.AddPackage(package);

            // Assert
            Assert.IsTrue(mockFileSystem.FileExists(@"A.1.0\A.1.0.nupkg"));
        }

        [TestMethod]
        public void RemovePackageRemovesPackageFileAndDirectoryAndRoot() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(@"A.1.0\A.1.0.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.AreEqual(3, mockFileSystem.Deleted.Count);
            Assert.IsTrue(mockFileSystem.Deleted.Contains(""));
            Assert.IsTrue(mockFileSystem.Deleted.Contains("A.1.0"));
            Assert.IsTrue(mockFileSystem.Deleted.Contains(@"A.1.0\A.1.0.nupkg"));
        }

        [TestMethod]
        public void RemovePackageDoesNotRemovesRootIfNotEmpty() {
            // Arrange
            var mockFileSystem = new MockProjectSystem();
            mockFileSystem.AddFile(@"A.1.0\A.1.0.nupkg");
            mockFileSystem.AddFile(@"B.1.0\B.1.0.nupkg");
            var repository = new LocalPackageRepository(new DefaultPackagePathResolver(mockFileSystem),
                                                        mockFileSystem);
            IPackage package = PackageUtility.CreatePackage("A", "1.0");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.AreEqual(2, mockFileSystem.Deleted.Count);
            Assert.IsTrue(mockFileSystem.Deleted.Contains("A.1.0"));
            Assert.IsTrue(mockFileSystem.Deleted.Contains(@"A.1.0\A.1.0.nupkg"));
        }

        private static DateTimeOffset GetDateTimeOffset(int seconds) {
            return new DateTimeOffset(1000, 10, 1, 0, 0, seconds, TimeSpan.Zero);
        }
    }
}
