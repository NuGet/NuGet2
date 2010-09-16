using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace NuPack.TeamFoundationServer {
    public interface ITfsWorkspace {
        bool PendEdit(string fullPath);
        bool PendAdd(string fullPath);
        bool PendDelete(string fullPath, RecursionType recursionType);
        string GetLocalItemForServerItem(string path);
        bool ItemExists(string path);

        // Change the type to something mockable
        IEnumerable<string> GetItems(string path);
        IEnumerable<string> GetItems(string fullPath, ItemType itemType);

        // Change this type to something mockable
        IEnumerable<PendingChange> GetPendingChanges(string fullPath, RecursionType recursionType);
        IEnumerable<PendingChange> GetPendingChanges(string path);
        void Undo(IEnumerable<PendingChange> pendingChange);
    }
}
