using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using TFS = Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer {
    public class TfsWorkspaceWrapper : ITfsWorkspace {
        private readonly Workspace _workspace;
        public TfsWorkspaceWrapper(Workspace workspace) {
            _workspace = workspace;
        }

        public bool PendEdit(string fullPath) {
            return _workspace.PendEdit(fullPath) > 0;
        }

        public bool PendAdd(string fullPath) {
            return _workspace.PendAdd(fullPath) > 0;
        }

        public IEnumerable<string> GetItems(string fullPath) {
            return GetItems(fullPath, ItemType.Any);
        }

        public IEnumerable<string> GetItems(string fullPath, ItemType itemType) {
            // REVIEW: We should pass the filter to this method so it happens on the server
            // REVIEW: Can we do some smart caching so we don't hit the server everytime this is called?
            var itemSet = _workspace.VersionControlServer.GetItems(fullPath, TFS.VersionSpec.Latest, RecursionType.OneLevel, DeletedState.NonDeleted, itemType);

            // Get the local files for the server files
            var items = new HashSet<string>(from item in itemSet.Items
                                            select GetLocalItemForServerItem(item.ServerItem),
                                            StringComparer.OrdinalIgnoreCase);

            // Remove the path from the list if we're looking for folders
            if (itemType == ItemType.Folder) {
                items.Remove(fullPath);

                if (Directory.Exists(fullPath)) {
                    // Files might have pending adds, but directories might not
                    // (even though they get added as a result of checking in those files).
                    foreach (var directory in Directory.EnumerateDirectories(fullPath)) {
                        items.Add(directory);
                    }
                }
            }

            // Remove pending deletes from the items collection
            foreach (var change in GetPendingChanges(fullPath, RecursionType.OneLevel)) {
                if (change.IsDelete) {
                    // Remove all children of this path
                    items.RemoveWhere(item => item.StartsWith(change.LocalItem, StringComparison.OrdinalIgnoreCase));
                }
                else {
                    items.Add(change.LocalItem);
                }
            }

            return items;
        }

        public IEnumerable<PendingChange> GetPendingChanges(string fullPath) {
            return _workspace.GetPendingChangesEnumerable(fullPath);
        }

        public IEnumerable<PendingChange> GetPendingChanges(string fullPath, RecursionType recursionType) {
            return _workspace.GetPendingChangesEnumerable(fullPath, recursionType);
        }

        public string GetLocalItemForServerItem(string path) {
            return _workspace.GetLocalItemForServerItem(path);
        }

        public void Undo(IEnumerable<PendingChange> pendingChanges) {
            if (pendingChanges.Any()) {
                _workspace.Undo(pendingChanges.ToArray());
            }
        }

        public bool PendDelete(string fullPath, RecursionType recursionType) {
            return _workspace.PendDelete(fullPath, recursionType) > 0;
        }


        public bool ItemExists(string path) {
            try {
                // TODO: Find a better way to implement this
                return GetItems(path).Any();
            }
            catch (ItemNotFoundException) {
            }
            catch (ItemNotMappedException) {
            }
            return false;
        }
    }
}
