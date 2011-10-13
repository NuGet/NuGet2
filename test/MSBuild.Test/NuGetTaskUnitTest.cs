using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Moq;
using NuGet.MSBuild;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.MSBuild
{

    public class NuGetTaskUnitTest
    {
        private const string createdPackage = "thePackageId.1.0.nupkg";
        private const string NuSpecFile = "thePackageId.nuspec";

        [Fact]
        public void WillUseCurrentDirectoryIfBaseDirNotSet()
        {
            var fileSystemProviderStub = new Mock<IFileSystemProvider>();
            var currentDir = Directory.GetCurrentDirectory();
            fileSystemProviderStub.Setup(c => c.CreateFileSystem(currentDir)).Returns(new MockFileSystem()).Verifiable();

            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(fileSystemProviderStub: fileSystemProviderStub);
            task.SpecFile = string.Empty;

            task.Execute();

            fileSystemProviderStub.Verify();
        }

        [Fact]
        public void WillUseBaseDirectoryIfBaseDirSet()
        {
            var fileSystemProviderStub = new Mock<IFileSystemProvider>();
            var baseDir = "/a/b/c";
            fileSystemProviderStub.Setup(c => c.CreateFileSystem(baseDir)).Returns(new MockFileSystem()).Verifiable();

            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(fileSystemProviderStub: fileSystemProviderStub);
            task.SpecFile = string.Empty;
            task.BaseDir = baseDir;

            task.Execute();

            fileSystemProviderStub.Verify();
        }

        [Fact]
        public void WillLogAnErrorWhenTheSpecFileIsEmpty()
        {
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            var fileSystemProviderStub = new Mock<IFileSystemProvider>();
            fileSystemProviderStub.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());

            NuGet.MSBuild.NuGet task = CreateTaskWithDefaultStubs(fileSystemProviderStub: fileSystemProviderStub, buildEngineStub: buildEngineStub);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            task.SpecFile = string.Empty;

            bool actualResut = task.Execute();

            Assert.Equal("The spec file must not be empty.", actualMessage);
            Assert.False(actualResut);
        }

        [Fact]
        public void OutputPathUsesPackageIdAndVerion()
        {
            // Arrange
            var packageBuilder = new PackageBuilder { Id = "Foo", Version = new SemanticVersion("1.1") };

            // Act
            var task = CreateTaskWithDefaultStubs();
            task.PackageDir = @"X:\";
            string outputPath = task.GetOutputPath(packageBuilder);

            // Assert
            Assert.Equal(@"X:\Foo.1.1.nupkg", outputPath);
        }

        [Fact]
        public void OutputPathAppendsSymbolPackageIdentifier()
        {
            // Arrange
            var packageBuilder = new PackageBuilder { Id = "Foo", Version = new SemanticVersion("1.1") };

            // Act
            var task = CreateTaskWithDefaultStubs();
            task.PackageDir = @"X:\";
            var outputPath = task.GetOutputPath(packageBuilder, symbols: true);

            // Assert
            Assert.Equal(@"X:\Foo.1.1.symbols.nupkg", outputPath);
        }

        [Fact]
        public void WillErrorWhenTheSpecFileDoesNotExist()
        {
            // Arrange
            string actualMessage = null;
            var fileSystemProviderStub = new Mock<IFileSystemProvider>();
            fileSystemProviderStub.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var buildEngineStub = new Mock<IBuildEngine>();
            var task = CreateTaskWithDefaultStubs(fileSystemProviderStub: fileSystemProviderStub, buildEngineStub: buildEngineStub);
            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);
            task.SpecFile = "aPathThatDoesNotExist";

            // Act
            bool actualResut = task.Execute();

            // Assert
            Assert.Equal("The spec file does not exist.", actualMessage);
            Assert.False(actualResut);
        }

        [Fact]
        public void WillRemoveNuspecFilesFromPackage()
        {
            // Arrange
            var regularFile = new PhysicalPackageFile { SourcePath = @"C:\readme.txt", TargetPath = @"content\readme.txt" };
            var packageFiles = new List<IPackageFile> { 
                new PhysicalPackageFile { SourcePath = @"C:\foo.nuspec", TargetPath = "foo.nuspec" },
                regularFile
            };

            // Act
            var task = CreateTaskWithDefaultStubs(fileSystemRoot: @"C:\");
            task.BaseDir = @"C:\";
            task.ExcludeFiles(packageFiles);

            // Assert
            Assert.Equal(1, packageFiles.Count);
            Assert.Equal(regularFile, packageFiles.Single());
        }

        [Fact]
        public void WillRemoveNupkgFilesFromPackage()
        {
            // Arrange
            var regularFile = new PhysicalPackageFile { SourcePath = @"C:\readme.txt", TargetPath = @"content\readme.txt" };
            var packageFiles = new List<IPackageFile> { 
                new PhysicalPackageFile { SourcePath = @"C:\foo.nupkg", TargetPath = "foo.nupkg" },
                regularFile
            };

            // Act
            var task = CreateTaskWithDefaultStubs(fileSystemRoot: @"C:\");
            task.BaseDir = @"C:\";
            task.ExcludeFiles(packageFiles);

            // Assert
            Assert.Equal(1, packageFiles.Count);
            Assert.Equal(regularFile, packageFiles.Single());
        }

        [Fact]
        public void WillNotRemoveLibraryFilesFromPackage()
        {
            // Arrange
            var packageFiles = new List<IPackageFile> { 
                new PhysicalPackageFile { SourcePath = @"C:\foo.dll", TargetPath = @"lib\foo.dll" },
                new PhysicalPackageFile { SourcePath = @"C:\readme.txt", TargetPath = @"content\readme.txt" }
            };

            // Act
            var task = CreateTaskWithDefaultStubs(fileSystemRoot: @"C:\");
            task.BaseDir = @"C:\";
            task.ExcludeFiles(packageFiles);

            // Assert
            Assert.Equal(2, packageFiles.Count);
        }

        [Fact]
        public void WillLogAnErrorWhenAnUnexpectedErrorHappens()
        {
            string actualMessage = null;
            var buildEngineStub = new Mock<IBuildEngine>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(c => c.FileExists(It.IsAny<string>())).Returns(true);
            fileSystem.Setup(c => c.DirectoryExists(It.IsAny<string>())).Returns(true);
            fileSystem.Setup(c => c.OpenFile("package.nuspec")).Returns(@"<xml version=""1.0"">".AsStream());
            fileSystemProvider.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(fileSystem.Object);
            var task = CreateTaskWithDefaultStubs(buildEngineStub: buildEngineStub, fileSystemProviderStub: fileSystemProvider);

            buildEngineStub
                .Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(e => actualMessage = e.Message);

            bool actualResut = task.Execute();

            Assert.True(actualMessage.Contains("An unexpected error occurred while creating the package:"));
            Assert.False(actualResut);
        }

        private static NuGet.MSBuild.NuGet CreateTaskWithDefaultStubs(
                                                              Mock<IFileSystemProvider> fileSystemProviderStub = null,
                                                              Mock<IBuildEngine> buildEngineStub = null,
                                                              string fileSystemRoot = "/")
        {
            if (fileSystemProviderStub == null)
            {
                fileSystemProviderStub = new Mock<IFileSystemProvider>();
                var mockFileSystem = new MockFileSystem();
                mockFileSystem.AddFile(fileSystemRoot + NuSpecFile);
                fileSystemProviderStub.Setup(c => c.CreateFileSystem(It.IsAny<string>())).Returns(mockFileSystem);
            }

            if (buildEngineStub == null)
            {
                buildEngineStub = new Mock<IBuildEngine>();
            }

            var task = new NuGet.MSBuild.NuGet(fileSystemProviderStub.Object);

            task.BuildEngine = buildEngineStub.Object;
            task.SpecFile = fileSystemRoot + "thePackageId.nuspec";

            return task;
        }
    }
}
