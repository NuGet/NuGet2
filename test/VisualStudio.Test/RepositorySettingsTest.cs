using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio.Test.Mocks;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class RepositorySettingsTest {
        [TestMethod]
        public void CtorWithNullSolutionManagerThrows() {
            ExceptionAssert.ThrowsArgNull(() => new RepositorySettings(null, new Mock<IFileSystemProvider>().Object), "solutionManager");
        }

        [TestMethod]
        public void RepositoryPathThrowsIfSolutionDirectoryIsNull() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var repositorySettings = new RepositorySettings(solutionManager.Object, new Mock<IFileSystemProvider>().Object);
            
            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => { string s = repositorySettings.RepositoryPath; }, "Unable to locate the solution directory. Please ensure that the solution has been saved.");
        }

        [TestMethod]
        public void RepositoryPathDefaultsToPackagesFolderInSolutionDirectoryIfNoConfigExists() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(new MockFileSystem());
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.AreEqual(@"bar\baz\packages", path);
        }

        [TestMethod]
        public void RepositoryPathComesFromConfigFileIfSpecified() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>lib</repositoryPath>
</settings>");
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.AreEqual(@"bar\lib", path);
        }

        [TestMethod]
        public void RepositoryPathDefaultsToPackagesDirectoryIfConfigFileHasEmptyOrNullRepositoryPath() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath></repositoryPath>
</settings>");
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.AreEqual(@"bar\baz\packages", path);
        }

        [TestMethod]
        public void RepositoryPathMalformedConfigThrows() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath
</settings>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => { string s = repositorySettings.RepositoryPath; }, @"Error reading 'bar\nuget.config'.");
        }

        [TestMethod]
        public void ConfigFoundInDirectoryHierarchyIsCached() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>lib</repositoryPath>
</settings>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.AddFile(@"bar\baz\nuget.config", @"
<settings>
    <repositoryPath>foo</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.AreEqual(@"bar\lib", p1);
            Assert.AreEqual(@"bar\lib", p2);
        }

        [TestMethod]
        public void OnlyConfigPathIsCachedNotRepositoryPath() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>lib</repositoryPath>
</settings>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>..\..\lib</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.AreEqual(@"bar\lib", p1);
            Assert.AreEqual(@"bar\..\..\lib", p2);
        }

        [TestMethod]
        public void ConfigurationCacheIsClearedIfFileRemoved() {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>lib</repositoryPath>
</settings>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.DeleteFile(@"bar\nuget.config");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.AreEqual(@"bar\lib", p1);
            Assert.AreEqual(@"bar\baz\packages", p2);
        }

        [TestMethod]
        public void ConfigurationCacheIsClearedIfSolutionCloses() {
            // Arrange
            var solutionManager = new Mock<MockSolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>lib</repositoryPath>
</settings>");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(fileSystem);
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            solutionManager.Object.CloseSolution();
            fileSystem.AddFile(@"bar\baz\nuget.config", @"
<settings>
    <repositoryPath>foo</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.AreEqual(@"bar\lib", p1);
            Assert.AreEqual(@"bar\baz\foo", p2);
        }
    }
}
