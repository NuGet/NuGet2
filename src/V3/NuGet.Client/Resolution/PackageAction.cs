using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;

namespace NuGet.Client.Resolution
{
    /// <summary>
    /// Represents an action that needs to be taken on a package.
    /// </summary>
    public class PackageAction
    {
        public PackageIdentity PackageIdentity { get; private set; }
        public JObject Package { get; private set; }
        public InstallationTarget Target { get; private set; }
        public PackageActionType ActionType { get; private set; }
        public SourceRepository Source { get; private set; }
        public PackageIdentity DependentPackage { get; private set; }
        public bool IsUpdate { get; set; }

        public PackageAction(PackageActionType actionType, PackageIdentity packageName, JObject package, InstallationTarget target, SourceRepository source, PackageIdentity dependentPackage)
        {
            ActionType = actionType;
            PackageIdentity = packageName;
            Package = package;
            Target = target;
            Source = source;
            DependentPackage = dependentPackage;
            IsUpdate = false;
        }

        public override string ToString()
        {
            return 
                ActionType.ToString() + " " + PackageIdentity.ToString() + 
                (Target == null ? String.Empty : (" " + Target.Name));
        }
    }   
}
