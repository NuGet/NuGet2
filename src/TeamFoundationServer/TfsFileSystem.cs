using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer {
    public class TfsFileSystem : PhysicalFileSystem {
        public TfsFileSystem(Workspace workspace, string path)
            : this(new TfsWorkspaceWrapper(workspace), path) {
        }

        public TfsFileSystem(ITfsWorkspace workspace, string path)
            : base(path) {
            Workspace = workspace;
        }

        public ITfsWorkspace Workspace { get; private set; }

        public override void AddFile(string path, Stream stream) {
            string fullPath = GetFullPath(path);
            // See if there are any pending changes for this file
            var pendingChanges = Workspace.GetPendingChanges(fullPath);
            bool pendingAddOrEdit = pendingChanges.Any(c => c.IsEdit || c.IsAdd);

            // Undo all pending deletes
            var pendingDeletes = pendingChanges.Where(p => p.IsDelete);
            Workspace.Undo(pendingDeletes);

            if (base.FileExists(path) && !pendingAddOrEdit) {
                // If the file exists, but there is not pending edit then edit the file (if it is under source control)
                pendingAddOrEdit = Workspace.PendEdit(fullPath);
            }

            base.AddFile(path, stream);

            if (!pendingAddOrEdit) {
                Workspace.PendAdd(fullPath);
            }
        }

        public override void DeleteFile(string path) {
            if (!DeleteItem(path, RecursionType.None)) {
                // If this file wasn't deleted, call base to remove it from the disk
                base.DeleteFile(path);
            }
        }

        public override void DeleteDirectory(string path, bool recursive = false) {
            if (!DeleteItem(path, RecursionType.None)) {
                // If no files were deleted, call base to remove them from the disk
                base.DeleteDirectory(path, recursive);
            }
        }

        private bool DeleteItem(string path, RecursionType recursionType) {
            string fullPath = GetFullPath(path);

            var pendingChanges = Workspace.GetPendingChanges(fullPath);
            // If there are any pending deletes then do nothing
            if (pendingChanges.Any(c => c.IsDelete)) {
                return true;
            }

            // Undo other pending changes
            Workspace.Undo(pendingChanges);
            return Workspace.PendDelete(fullPath, recursionType);
        }

        protected override void EnsureDirectory(string path) {
            base.EnsureDirectory(path);
            Workspace.PendAdd(GetFullPath(path));
        }
    }
}
