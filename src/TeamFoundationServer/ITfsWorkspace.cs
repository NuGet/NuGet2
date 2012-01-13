using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuGet.TeamFoundationServer
{
    public interface ITfsWorkspace
    {
        bool PendEdit(string fullPath);
        bool PendAdd(string fullPath);
        bool PendDelete(string fullPath, RecursionType recursionType);
        bool PendDelete(IEnumerable<string> paths, RecursionType recursionType);
        string GetLocalItemForServerItem(string path);
        bool ItemExists(string path, ItemType itemType);

        // Change the type to something mockable
        IEnumerable<string> GetItems(string fullPath);
        IEnumerable<string> GetItems(string fullPath, bool excludePendingDeletes);

        // Change this type to something mockable
        IEnumerable<PendingChange> GetPendingChanges(string fullPath, RecursionType recursionType);
        IEnumerable<PendingChange> GetPendingChanges(string fullPath);
        void Undo(IEnumerable<PendingChange> pendingChanges);
    }
}
