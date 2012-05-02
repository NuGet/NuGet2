using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;
using Moq;
using Xunit;

namespace NuGet.TeamFoundationServer
{
    public class TfsFileSystemTest
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfWorkspaceIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsFileSystem(null as Workspace, Path.GetRandomFileName()));
            Assert.Equal<string>("workspace", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfWorkspaceInterfaceIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsFileSystem(null as ITfsWorkspace, Path.GetRandomFileName()));
            Assert.Equal<string>("workspace", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorForWorkspaceInterfaceInitializesInstance()
        {
            var workspace = Mock.Of<ITfsWorkspace>();
            var root = Path.GetRandomFileName();

            var target = new TfsFileSystem(workspace, root);

            Assert.Equal<string>(root, target.Root, StringComparer.Ordinal);
            Assert.True(object.ReferenceEquals(workspace, target.Workspace), "TfsFileSystem.Workspace is incorrect.");
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsNull()
        {
            var exception = Assert.Throws<ArgumentException>(() => new TfsFileSystem(Mock.Of<ITfsWorkspace>(), null));
            Assert.Equal<string>("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void ConstructorThrowsArgumentExceptionIfRootIsTheEmptyString()
        {
            var exception = Assert.Throws<ArgumentException>(() => new TfsFileSystem(Mock.Of<ITfsWorkspace>(), string.Empty));
            Assert.Equal<string>("root", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void AddFileThrowsArgumentNullExceptionIfStreamIsNull()
        {
            var workspace = Mock.Of<ITfsWorkspace>();
            var root = Path.GetRandomFileName();
            var target = new TfsFileSystem(workspace, root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.AddFile(Path.GetRandomFileName(), null));
            Assert.Equal<string>("stream", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void FileAddedIfPendingDelete()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.GetRandomFileName();
            string fullPath = Path.Combine(root, path);

            Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
            change.Setup((p) => p.LocalItem).Returns(fullPath);
            change.Setup((p) => p.IsDelete).Returns(true);

            var pendingChanges = new ITfsPendingChange[]
            {
               change.Object,
            };

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(pendingChanges);

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns((string i) => pendingChanges.Where(p => string.Equals(p.LocalItem, i, StringComparison.Ordinal)).Any());

            var target = new TfsFileSystem(workspace.Object, root);

            try
            {
                // Create some random content for the member being added
                string content = Guid.NewGuid().ToString();

                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    target.AddFile(path, stream);
                }

                workspace.Verify(w => w.PendAdd(fullPath), Times.Never());
                workspace.Verify(w => w.ItemExists(fullPath), Times.Once());
                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendEdit(fullPath), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                // Validate the file was added with the correct content
                Assert.True(Directory.Exists(root));
                Assert.True(File.Exists(fullPath));

                string written = File.ReadAllText(fullPath);
                Assert.Equal<string>(content, written, StringComparer.Ordinal);
            }
            finally
            {
                // Tidy up
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void FileAddedIfPendingAdd()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.GetRandomFileName();
            string fullPath = Path.Combine(root, path);

            Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
            change.Setup((p) => p.LocalItem).Returns(fullPath);
            change.Setup((p) => p.IsAdd).Returns(true);

            var pendingChanges = new ITfsPendingChange[]
            {
                change.Object,
            };

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(pendingChanges);

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            try
            {
                // Create some random content for the member being added
                string content = Guid.NewGuid().ToString();

                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    target.AddFile(path, stream);
                }

                workspace.Verify(w => w.PendAdd(fullPath), Times.Never());
                workspace.Verify(w => w.ItemExists(fullPath), Times.Once());
                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendEdit(fullPath), Times.Never());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                // Validate the file was added with the correct content
                Assert.True(Directory.Exists(root));
                Assert.True(File.Exists(fullPath));

                string written = File.ReadAllText(fullPath);
                Assert.Equal<string>(content, written, StringComparer.Ordinal);
            }
            finally
            {
                // Tidy up
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void FileAddedIfNoPendingAddAndNotInSourceControl()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.GetRandomFileName();
            string fullPath = Path.Combine(root, path);

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(Enumerable.Empty<ITfsPendingChange>());

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(false);

            var target = new TfsFileSystem(workspace.Object, root);

            try
            {
                // Create some random content for the member being added
                string content = Guid.NewGuid().ToString();

                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    target.AddFile(path, stream);
                }

                workspace.Verify(w => w.PendAdd(fullPath), Times.Once());
                workspace.Verify(w => w.ItemExists(fullPath), Times.Once());
                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendEdit(fullPath), Times.Never());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                // Validate the file was added with the correct content
                Assert.True(Directory.Exists(root));
                Assert.True(File.Exists(fullPath));

                string written = File.ReadAllText(fullPath);
                Assert.Equal<string>(content, written, StringComparer.Ordinal);
            }
            finally
            {
                // Tidy up
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void FileAddedIfPendingEdit()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.GetRandomFileName();
            string fullPath = Path.Combine(root, path);

            Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
            change.Setup((p) => p.LocalItem).Returns(fullPath);
            change.Setup((p) => p.IsEdit).Returns(true);

            var pendingChanges = new ITfsPendingChange[]
            {
                change.Object,
            };

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(pendingChanges);

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            try
            {
                // Create some random content for the member being added
                string content = Guid.NewGuid().ToString();

                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    target.AddFile(path, stream);
                }

                workspace.Verify(w => w.PendAdd(fullPath), Times.Never());
                workspace.Verify(w => w.ItemExists(fullPath), Times.Once());
                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendEdit(fullPath), Times.Never());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                // Validate the file was added with the correct content
                Assert.True(Directory.Exists(root));
                Assert.True(File.Exists(fullPath));

                string written = File.ReadAllText(fullPath);
                Assert.Equal<string>(content, written, StringComparer.Ordinal);
            }
            finally
            {
                // Tidy up
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }
        
        [Fact]
        public void FileAddedIfNoPendingChanges()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string path = Path.GetRandomFileName();
            string fullPath = Path.Combine(root, path);

            Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
            change.Setup((p) => p.LocalItem).Returns(fullPath);

            var pendingChanges = new ITfsPendingChange[]
            {
                change.Object,
            };

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(pendingChanges);

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            try
            {
                // Create some random content for the member being added
                string content = Guid.NewGuid().ToString();

                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    target.AddFile(path, stream);
                }

                workspace.Verify(w => w.PendAdd(fullPath), Times.Never());
                workspace.Verify(w => w.ItemExists(fullPath), Times.Once());
                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendEdit(fullPath), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                // Validate the file was added with the correct content
                Assert.True(Directory.Exists(root));
                Assert.True(File.Exists(fullPath));

                string written = File.ReadAllText(fullPath);
                Assert.Equal<string>(content, written, StringComparer.Ordinal);
            }
            finally
            {
                // Tidy up
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void FileExistsThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.FileExists(null));
            Assert.Equal<string>("path", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void FileExistsReturnsFalseIfPathIsEmptyString()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            bool result = target.FileExists(string.Empty);

            Assert.False(result);
        }

        [Fact]
        public void FileExistsReturnsFalseIfFileDoesNotExist()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);
            var path = Path.Combine(root, Path.GetRandomFileName());

            bool result = target.FileExists(path);

            Assert.False(result);
        }

        [Fact]
        public void FileExistsReturnsTrueIfFileExistsOnDiskAndNoPendingDelete()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                var workspace = new Mock<ITfsWorkspace>();

                Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
                change.Setup((p) => p.LocalItem).Returns(path);
                change.Setup((p) => p.IsEdit).Returns(true);

                var pendingChanges = new ITfsPendingChange[]
                {
                    change.Object,
                };

                workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(pendingChanges);

                workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                    .Returns(true);

                var target = new TfsFileSystem(workspace.Object, root);

                bool result = target.FileExists(fileName);

                Assert.True(result, "TfsFileSystem failed to find file on disk.");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void FileExistsReturnsFalseIfFileExistsOnDiskWithPendingDelete()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                var workspace = new Mock<ITfsWorkspace>();

                Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
                change.Setup((p) => p.LocalItem).Returns(path);
                change.Setup((p) => p.IsDelete).Returns(true);

                var pendingChanges = new ITfsPendingChange[]
                {
                    change.Object,
                };

                workspace.Setup(r => r.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(pendingChanges);

                workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                    .Returns(true);

                var target = new TfsFileSystem(workspace.Object, root);

                bool result = target.FileExists(fileName);

                Assert.False(result, "TfsFileSystem found file on disk with pending delete.");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void DeleteFileThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.DeleteFile(null));
            Assert.Equal<string>("path", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void DeleteFileOnDiskButNotInSourceControl()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var workspace = new Mock<ITfsWorkspace>();

                workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(Enumerable.Empty<ITfsPendingChange>());

                var target = new TfsFileSystem(workspace.Object, root);

                target.DeleteFile(path);

                workspace.Verify(w => w.GetPendingChanges(path, RecursionType.None), Times.Exactly(2));
                workspace.Verify(w => w.PendDelete(path, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                Assert.False(File.Exists(path), "The file was not deleted.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void DeleteFileOnDiskWithPendingDelete()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var workspace = new Mock<ITfsWorkspace>();

                Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
                change.Setup((p) => p.LocalItem).Returns(path);
                change.Setup((p) => p.IsDelete).Returns(true);

                var pendingChanges = new ITfsPendingChange[]
                {
                    change.Object,
                };

                workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(pendingChanges);

                var target = new TfsFileSystem(workspace.Object, root);

                target.DeleteFile(path);

                workspace.Verify(w => w.GetPendingChanges(path, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendDelete(path, RecursionType.None), Times.Never());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Never());

                Assert.True(File.Exists(path), "The file was deleted from disk when delete was pending.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void DeleteFileOnDiskWithNoPendingDelete()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var workspace = new Mock<ITfsWorkspace>();

                workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(Enumerable.Empty<ITfsPendingChange>());

                var target = new TfsFileSystem(workspace.Object, root);

                target.DeleteFile(path);

                workspace.Verify(w => w.GetPendingChanges(path, RecursionType.None), Times.Exactly(2));
                workspace.Verify(w => w.PendDelete(path, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                Assert.False(File.Exists(path), "The file was not deleted from disk.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void DeleteFileOnDiskWithPendingAdd()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var workspace = new Mock<ITfsWorkspace>();

                Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
                change.Setup((p) => p.LocalItem).Returns(path);
                change.Setup((p) => p.IsAdd).Returns(true);

                var pendingChanges = new ITfsPendingChange[]
                {
                    change.Object,
                };

                workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(pendingChanges);

                var target = new TfsFileSystem(workspace.Object, root);

                target.DeleteFile(path);

                workspace.Verify(w => w.GetPendingChanges(path, RecursionType.None), Times.Exactly(2));
                workspace.Verify(w => w.PendDelete(path, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                Assert.False(File.Exists(path), "The file was not deleted from disk when add was pending.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void DeleteFileOnDiskWithPendingEdit()
        {
            var path = Path.GetTempFileName();

            try
            {
                var root = Path.GetDirectoryName(path);
                var workspace = new Mock<ITfsWorkspace>();

                Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
                change.Setup((p) => p.LocalItem).Returns(path);
                change.Setup((p) => p.IsEdit).Returns(true);

                var pendingChanges = new ITfsPendingChange[]
                {
                    change.Object,
                };

                workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                    .Returns(pendingChanges);

                var target = new TfsFileSystem(workspace.Object, root);

                target.DeleteFile(path);

                workspace.Verify(w => w.GetPendingChanges(path, RecursionType.None), Times.Exactly(2));
                workspace.Verify(w => w.PendDelete(path, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                Assert.False(File.Exists(path), "The file was not deleted from disk when add was pending.");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void BindToSourceControlThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.BindToSourceControl(null));
            Assert.Equal<string>("paths", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void BindToSourceControlAddsPaths()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

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
        public void IsSourceControlBoundThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.IsSourceControlBound(null));
            Assert.Equal<string>("path", exception.ParamName, StringComparer.Ordinal);
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

            workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
            workspace.Verify(w => w.PendDelete(fullPath, RecursionType.None), Times.Once());
            workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());
        }

        [Fact]
        public void DeleteDirectoryIfPathExistsButNotInSourceControl()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();
            var fullPath = Path.Combine(root, path);

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(false);

            var target = new TfsFileSystem(workspace.Object, root);

            Directory.CreateDirectory(fullPath);

            try
            {
                target.DeleteDirectory(path);

                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendDelete(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                bool exists = Directory.Exists(fullPath);

                Assert.False(exists, "The directory was not deleted.");
            }
            finally
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void DeleteDirectoryIfPathExistsAndNoPendingDeletes()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();
            var fullPath = Path.Combine(root, path);

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            Directory.CreateDirectory(fullPath);

            try
            {
                target.DeleteDirectory(path);

                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendDelete(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Once());

                bool exists = Directory.Exists(fullPath);

                Assert.False(exists, "The directory was not deleted.");
            }
            finally
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void DeleteDirectoryIfPathExistsAndHasPendingDelete()
        {
            var root = Path.GetTempPath();
            var path = Path.GetRandomFileName();
            var fullPath = Path.Combine(root, path);

            Mock<ITfsPendingChange> change = new Mock<ITfsPendingChange>();
            change.Setup((p) => p.LocalItem).Returns(fullPath);
            change.Setup((p) => p.IsDelete).Returns(true);

            var pendingChanges = new ITfsPendingChange[]
            {
                change.Object,
            };

            var workspace = new Mock<ITfsWorkspace>();

            workspace.Setup(w => w.GetPendingChanges(It.IsAny<string>(), It.IsAny<RecursionType>()))
                .Returns(pendingChanges);

            workspace.Setup(w => w.ItemExists(It.IsAny<string>()))
                .Returns(true);

            var target = new TfsFileSystem(workspace.Object, root);

            Directory.CreateDirectory(fullPath);

            try
            {
                target.DeleteDirectory(path);

                workspace.Verify(w => w.GetPendingChanges(fullPath, RecursionType.None), Times.Once());
                workspace.Verify(w => w.PendDelete(fullPath, RecursionType.None), Times.Never());
                workspace.Verify(w => w.Undo(It.IsAny<IEnumerable<ITfsPendingChange>>()), Times.Never());

                bool exists = Directory.Exists(fullPath);

                Assert.True(exists, "The directory was deleted with a pending delete.");
            }
            finally
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        [Fact]
        public void BeginProcessingThrowsArgumentNullExceptionIfPathIsNull()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);

            var exception = Assert.Throws<ArgumentNullException>(() => target.BeginProcessing(null, PackageAction.Install));
            Assert.Equal<string>("batch", exception.ParamName, StringComparer.Ordinal);
        }

        [Fact]
        public void BeginProcessingAllowsNullBatchForUninstall()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var target = new TfsFileSystem(Mock.Of<ITfsWorkspace>(), root);
            
            target.BeginProcessing(null, PackageAction.Uninstall);
        }
    }
}