using System.Collections.Generic;
using System.Linq;
using NuGet.Resolver;

namespace NuGet.PowerShell.Commands
{
    internal class ReinstallInfo
    {
        public List<VirtualProjectManager> VirtualProjectManagers { get; private set; }

        public Dictionary<SemanticVersion, bool> VersionsChecked { get; private set; }

        public Dictionary<VirtualProjectManager, IPackage> PackagesInProject { get; private set; }

        public List<Operation> ProjectOperations { get; private set; }

        public ReinstallInfo(IEnumerable<VirtualProjectManager> virtualProjectManagers)
        {
            VirtualProjectManagers = virtualProjectManagers.ToList();
            VersionsChecked = new Dictionary<SemanticVersion, bool>();
            PackagesInProject = new Dictionary<VirtualProjectManager, IPackage>();
            ProjectOperations = new List<Operation>();
        }
    }
}
