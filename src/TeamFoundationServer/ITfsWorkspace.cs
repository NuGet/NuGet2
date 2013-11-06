using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer
{
    public interface ITfsWorkspace
    {
        bool PendEdit(string fullPath);
        bool PendAdd(string fullPath);
        bool PendAdd(IEnumerable<string> fullPaths);
        bool PendDelete(string fullPath, RecursionType recursionType);
        bool PendDelete(IEnumerable<string> fullPaths, RecursionType recursionType);
        string GetLocalItemForServerItem(string path);
        bool ItemExists(string fullPath);

        // Change the type to something mockable
        IEnumerable<string> GetItems(string fullPath);
        IEnumerable<string> GetItems(string fullPath, ItemType itemType);
        IEnumerable<string> GetItemsRecursive(string fullPath);
        IEnumerable<ITfsPendingChange> GetPendingChanges(string fullPath, RecursionType recursionType);
        IEnumerable<ITfsPendingChange> GetPendingChanges(string fullPath);
        IEnumerable<ITfsPendingChange> GetPendingChanges(IEnumerable<string> files);
        void Undo(IEnumerable<ITfsPendingChange> pendingChanges);
    }
}