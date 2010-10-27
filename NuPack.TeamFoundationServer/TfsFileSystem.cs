using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer {
    public class TfsFileSystem : FileBasedProjectSystem {
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

        public override IEnumerable<string> GetFiles(string path, string filter) {
            path = GetFullPath(path);
            Regex matcher = GetFilterRegex(filter);
            return Workspace.GetItems(path, ItemType.File)
                            .Where(file => matcher.IsMatch(file))
                            .Select(MakeRelativePath);
        }

        public override IEnumerable<string> GetDirectories(string path) {
            path = GetFullPath(path);
            return Workspace.GetItems(path, ItemType.Folder);
        }

        public override bool FileExists(string path) {
            return ItemExists(path) && base.FileExists(path);
        }

        public override bool DirectoryExists(string path) {
            return ItemExists(path) && base.DirectoryExists(path);
        }

        private bool ItemExists(string path) {
            return Workspace.ItemExists(GetFullPath(path));
        }

        // TODO: Move this logic to the base in a static protected method
        private static Regex GetFilterRegex(string wildcard) {
            string pattern = String.Join("\\.", wildcard.Split('.').Select(GetPattern));
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPattern(string token) {
            return token == "*" ? @"(.*)" : @"(" + token + ")";
        }
    }
}
