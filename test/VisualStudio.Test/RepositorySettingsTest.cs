using System;
using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{

    public class RepositorySettingsTest
    {
        [Fact]
        public void CtorWithNullSolutionManagerThrows()
        {
            ExceptionAssert.ThrowsArgNull(() => new RepositorySettings(null, new Mock<IFileSystemProvider>().Object, new Mock<IVsSourceControlTracker>().Object), "solutionManager");
        }

        [Fact]
        public void RepositoryPathThrowsIfSolutionDirectoryIsNull()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            var repositorySettings = new RepositorySettings(solutionManager.Object, new Mock<IFileSystemProvider>().Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => { string s = repositorySettings.RepositoryPath; }, "Unable to locate the solution directory. Please ensure that the solution has been saved.");
        }

        [Fact]
        public void RepositoryPathDefaultsToPackagesFolderInSolutionDirectoryIfNoConfigExists()
        {
            // Arrange
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.Setup(m => m.SolutionDirectory).Returns(@"bar\baz");
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(m => m.GetFileSystem(@"bar\baz")).Returns(new MockFileSystem());
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.Equal(@"bar\baz\packages", path);
        }

        [Fact]
        public void RepositoryPathComesFromConfigFileIfSpecified()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.Equal(@"bar\lib", path);
        }

        [Fact]
        public void RepositoryPathDefaultsToPackagesDirectoryIfConfigFileHasEmptyOrNullRepositoryPath()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string path = repositorySettings.RepositoryPath;

            // Assert
            Assert.Equal(@"bar\baz\packages", path);
        }

        [Fact]
        public void RepositoryPathMalformedConfigThrows()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => { string s = repositorySettings.RepositoryPath; }, @"Error reading 'bar\nuget.config'.");
        }

        [Fact]
        public void ConfigFoundInDirectoryHierarchyIsCached()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.AddFile(@"bar\baz\nuget.config", @"
<settings>
    <repositoryPath>foo</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.Equal(@"bar\lib", p1);
            Assert.Equal(@"bar\lib", p2);
        }

        [Fact]
        public void OnlyConfigPathIsCachedNotRepositoryPath()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.AddFile(@"bar\nuget.config", @"
<settings>
    <repositoryPath>..\..\lib</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.Equal(@"bar\lib", p1);
            Assert.Equal(@"bar\..\..\lib", p2);
        }

        [Fact]
        public void ConfigurationCacheIsClearedIfFileRemoved()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            fileSystem.DeleteFile(@"bar\nuget.config");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.Equal(@"bar\lib", p1);
            Assert.Equal(@"bar\baz\packages", p2);
        }

        [Fact]
        public void ConfigurationCacheIsClearedIfSolutionCloses()
        {
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
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, new Mock<IVsSourceControlTracker>().Object);

            // Act
            string p1 = repositorySettings.RepositoryPath;

            solutionManager.Object.CloseSolution();
            fileSystem.AddFile(@"bar\baz\nuget.config", @"
<settings>
    <repositoryPath>foo</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;


            // Assert
            Assert.Equal(@"bar\lib", p1);
            Assert.Equal(@"bar\baz\foo", p2);
        }

        [Fact]
        public void ConfigurationCacheIsClearedIfSourceControlBindingChanges()
        {
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

            var sourceControlTracker = new Mock<IVsSourceControlTracker>();
            var repositorySettings = new RepositorySettings(solutionManager.Object, fileSystemProvider.Object, sourceControlTracker.Object);
            

            // Act
            string p1 = repositorySettings.RepositoryPath;

            sourceControlTracker.Raise(p => p.SolutionBoundToSourceControl += (o, e) => { }, EventArgs.Empty);
            
            fileSystem.AddFile(@"bar\baz\nuget.config", @"
<settings>
    <repositoryPath>foo</repositoryPath>
</settings>");

            string p2 = repositorySettings.RepositoryPath;

            // Assert
            Assert.Equal(@"bar\lib", p1);
            Assert.Equal(@"bar\baz\foo", p2);
        }
    }
}
