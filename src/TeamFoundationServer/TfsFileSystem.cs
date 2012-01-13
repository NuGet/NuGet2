using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer
{
    public class TfsFileSystem : PhysicalFileSystem
    {
        public TfsFileSystem(Workspace workspace, string path)
            : this(new TfsWorkspaceWrapper(workspace), path)
        {
        }

        public TfsFileSystem(ITfsWorkspace workspace, string path)
            : base(path)
        {
            Workspace = workspace;
        }

        public ITfsWorkspace Workspace { get; private set; }

        public override void AddFile(string path, Stream stream)
        {
            string fullPath = GetFullPath(path);

            // If any of the ancestor directories for a file are deleted, we need to undo the delete prior to adding the file.
            // This happens when updating content files where the parent directory could be deleted by the uninstall operation.
            UndoAncestorPendingDeletes(fullPath);

            // See if there are any pending changes for this file
            var pendingChanges = Workspace.GetPendingChanges(fullPath);
            var pendingDeletes = pendingChanges.Where(p => p.IsDelete);

            bool requiresEdit = pendingDeletes.Any() || !pendingChanges.Any(c => c.IsEdit || c.IsAdd);

            // Undo all pending deletes
            Workspace.Undo(pendingDeletes);

            // If the file was marked as deleted, and we undid the change or has no pending adds or edits, we need to edit it.
            if (requiresEdit && base.FileExists(path) && (pendingDeletes.Any() || Workspace.ItemExists(path, ItemType.File)))
            {
                // If the file exists, but there is not pending edit then edit the file (if it is under source control)
                requiresEdit = !Workspace.PendEdit(fullPath);
            }

            base.AddFile(path, stream);

            if (requiresEdit)
            {
                EnsureDirectory(Path.GetDirectoryName(path));
                Workspace.PendAdd(fullPath);
            }
        }

        private void UndoAncestorPendingDeletes(string path)
        {
            var visitedPaths = new Stack<string>();
            do
            {
                path = Path.GetDirectoryName(path);
                if (GetPendingDeletes(path).Any())
                {
                    visitedPaths.Push(path);
                    // If one or more subdirectories were deleted, convert directory deletes to individual file deletes.
                    ExpandDirectoryDelete(visitedPaths);
                    break;
                }
                visitedPaths.Push(path);
            } while (PathUtility.IsSubdirectory(Root, path));
        }

        private void ExpandDirectoryDelete(Stack<string> visitedPaths)
        {
            while (visitedPaths.Any())
            {
                string path = visitedPaths.Peek();
                var pendingDeletes = GetPendingDeletes(path);
                Workspace.Undo(pendingDeletes);

                // Now add all the child nodes under it to delete.
                var items = Workspace.GetItems(path, excludePendingDeletes: false);
                
                var childItems = Enumerable.Except(items, visitedPaths, StringComparer.OrdinalIgnoreCase);
                Workspace.PendDelete(childItems, RecursionType.Full);
                
                visitedPaths.Pop();
            }
        }

        private IEnumerable<PendingChange> GetPendingDeletes(string path)
        {
            var pendingChanges = Workspace.GetPendingChanges(path);
            return pendingChanges.Where(c => c.IsDelete);
        }

        public override void DeleteFile(string path)
        {
            if (!DeleteItem(path, RecursionType.None))
            {
                // If this file wasn't deleted, call base to remove it from the disk
                base.DeleteFile(path);
            }
        }

        public override bool FileExists(string path)
        {
            var fullPath = GetFullPath(path);
            return base.FileExists(path) && Workspace.ItemExists(fullPath, ItemType.File);
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            if (!DeleteItem(path, RecursionType.None))
            {
                // If no files were deleted, call base to remove them from the disk
                base.DeleteDirectory(path, recursive);
            }
        }

        private bool DeleteItem(string path, RecursionType recursionType)
        {
            string fullPath = GetFullPath(path);

            var pendingChanges = Workspace.GetPendingChanges(fullPath);
            // If there are any pending deletes then do nothing
            if (pendingChanges.Any(c => c.IsDelete))
            {
                return true;
            }

            // Undo other pending changes
            Workspace.Undo(pendingChanges);
            return Workspace.PendDelete(fullPath, recursionType);
        }

        protected override void EnsureDirectory(string path)
        {
            base.EnsureDirectory(path);
            Workspace.PendAdd(GetFullPath(path));
        }
    }
}
