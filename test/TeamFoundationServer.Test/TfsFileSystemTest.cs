using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Moq;
using NuGet.Test;
using Xunit;

namespace NuGet.TeamFoundationServer
{
    public class TfsFileSystemTest
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfWorkspaceIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsFileSystem(null as Workspace, Path.GetRandomFileName()));
            Assert.Equal("workspace", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfWorkspaceInterfaceIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsFileSystem(null as ITfsWorkspace, Path.GetRandomFileName()));
            Assert.Equal("workspace", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorForWorkspaceInterfaceInitializesInstance()
        {
            var workspace = Mock.Of<ITfsWorkspace>();
            var root = Path.GetRandomFileName();

            var target = new TfsFileSystem(workspace, root);

            Assert.Equal(root, target.Root, StringComparer.Ordinal);
            Assert.Same(workspace, target.Workspace);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsNull()
        {
            var exception = Assert.Throws<ArgumentException>(() => new TfsFileSystem(Mock.Of<ITfsWorkspace>(), null));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsTheEmptyString()
        {
            var exception = Assert.Throws<ArgumentException>(() => new TfsFileSystem(Mock.Of<ITfsWorkspace>(), string.Empty));
            Assert.Equal("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfStreamIsNull()
        {
            var workspace = Mock.Of<ITfsWorkspace>();
            var root = Path.GetRandomFileName();
            var target = new TfsFileSystem(workspace, root);

            ExceptionAssert.ThrowsArgNull(() => target.AddFile(Path.GetRandomFileName(), stream: null), "stream");
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfWriteToStreamIsNull()
        {
            var workspace = Mock.Of<ITfsWorkspace>();
            var root = Path.GetRandomFileName();
            var target = new TfsFileSystem(workspace, root);

            ExceptionAssert.ThrowsArgNull(() => target.AddFile(Path.GetRandomFileName(), writeToStream: null), "writeToStream");
        }

        [Fact]
        public void BindToSourceControlThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.BindToSourceControl(null));
            Assert.Equal("paths", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void BindToSourceControlAddsPaths()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // Use short paths for the names to test that full paths are used
            var paths = new string[]
            {
                Path.GetRandomFileName(),
                Path.GetRandomFileName(),
                Path.GetRandomFileName(),
            };

            var workspace = new Mock<ITfsWorkspace>();

            var target = new TfsFileSystem(workspace.Object, root);

            target.BindToSourceControl(paths);

            var expectedPaths = new string[]
            {
                Path.Combine(root, paths[0]),
                Path.Combine(root, paths[1]),
                Path.Combine(root, paths[2]),
            };

            // Validate the full paths were used when binding
            workspace.Verify(w => w.PendAdd(It.Is<IEnumerable<string>>(v => !v.Except(expectedPaths).Any() )), Times.Once());
        }

        [Fact]
        public void IsSourceControlBoundReturnsFalseIfNotInWorkspace()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(false);

            var target = new TfsFileSystem(workspace.Object, root);

            bool result = target.IsSourceControlBound(path);

            workspace.Verify(w => w.ItemExists(Path.Combine(root, path)), Times.Once());

            Assert.False(result, "File was incorrectly determined to be source control bound.");
        }

        [Fact]
        public void IsSourceControlBoundReturnsTrueIfInWorkspace()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            bool result = target.IsSourceControlBound(path);

            workspace.Verify(w => w.ItemExists(Path.Combine(root, path)), Times.Once());

            Assert.True(result, "File was incorrectly determined to not be source control bound.");
        }

        [Fact]
        public void DeleteDirectoryIfPathDoesNotExist()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();
            var fullPath = Path.Combine(root, path);

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(false);

            var target = new TfsFileSystem(workspace.Object, root);

            target.DeleteDirectory(path);

            workspace.Verify(w => w.GetPendingChanges(fullPath), Times.Once());
            workspace.Verify(w => w.PendDelete(fullPath, RecursionType.None), Times.Once());
            workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());
        }

        [Fact]
        public void BeginProcessingThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.BeginProcessing(null, PackageAction.Install));
            Assert.Equal("batch", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void BeginProcessingAllowsNullBatchForUninstall()
        {
            // Arrange
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);
            
            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => target.BeginProcessing(null, PackageAction.Uninstall), "batch");
        }
    }
}