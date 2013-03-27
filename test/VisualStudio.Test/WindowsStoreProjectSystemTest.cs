using System;
using System.Collections.Generic;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class WindowsStoreProjectSystemTest
    {
        [Fact]
        public void WindowsStoreProjectSystemDoesNotAllowBindingRedirect()
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var project = TestUtils.GetProject("WindowsStore");

            var projectSystem = new WindowsStoreProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool isBindingRedirectSupported = projectSystem.IsBindingRedirectSupported;

            // Assert
            Assert.False(isBindingRedirectSupported);
        }

        [Theory]
        [InlineData("app.CONFIG")]
        [InlineData("dir\\app.config")]
        [InlineData("dir\\one\\App.Config")]
        public void WindowsStoreProjectSystemDoesNotAcceptAppConfigFile(string path)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var project = TestUtils.GetProject("WindowsStore");

            var projectSystem = new WindowsStoreProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool isFileSupported = projectSystem.IsSupportedFile(path);

            // Assert
            Assert.False(isFileSupported);
        }

        [Theory]
        [InlineData("web.CONFIG")]
        [InlineData("dir\\web.config")]
        [InlineData("dir\\one\\Web.release.Config")]
        public void WindowsStoreProjectSystemDoesNotAcceptWebConfigFile(string path)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var project = TestUtils.GetProject("WindowsStore");

            var projectSystem = new WindowsStoreProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool isFileSupported = projectSystem.IsSupportedFile(path);

            // Assert
            Assert.False(isFileSupported);
        }

        [Theory]
        [InlineData("web.txt")]
        [InlineData("dir\\readme.txt")]
        [InlineData("dir\\one\\config.app")]
        public void WindowsStoreProjectSystemAcceptAllOtherFiles(string path)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            var project = TestUtils.GetProject("WindowsStore");

            var projectSystem = new WindowsStoreProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool isFileSupported = projectSystem.IsSupportedFile(path);

            // Assert
            Assert.True(isFileSupported);
        }

        [Fact]
        public void InstallPackageIntoWindowsStoreProjectIgnoreAppConfigFile()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();

            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var project = TestUtils.GetProject("WindowsStore");

            var projectSystem = new TestableWindowsStoreProjectSystem(project, mockFileSystemProvider.Object);
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var package = NuGet.Test.PackageUtility.CreatePackage(
                "foo", "1.0.0", content: new[] { "readme.txt", "app.config" });
            sourceRepository.AddPackage(package);

            var projectManager = new ProjectManager(
                sourceRepository,
                pathResolver,
                projectSystem,
                new MockPackageRepository());

            // Act
            projectManager.AddPackageReference(package, ignoreDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(projectSystem.FileExists("readme.txt"));
            Assert.False(projectSystem.FileExists("app.config"));
        }

        private class TestableWindowsStoreProjectSystem : WindowsStoreProjectSystem
        {
            private HashSet<string> _files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public TestableWindowsStoreProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
                : base(project, fileSystemProvider)
            {
            }

            public override bool FileExists(string path)
            {
                return FileExistsInProject(path);
            }

            public override void AddFile(string path, System.IO.Stream stream)
            {
                AddFileToProject(path);
            }

            public override bool FileExistsInProject(string path)
            {
                return _files.Contains(path);
            }

            protected override void AddFileToProject(string path)
            {
                _files.Add(path);
            }
        }
    }
}