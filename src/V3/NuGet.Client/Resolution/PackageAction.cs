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

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IPackage CorePackage { get; private set; }

        public PackageAction(PackageActionType actionType, PackageIdentity packageName, JObject package, InstallationTarget target)
            : this(actionType, packageName, package, target, corePackage: null)
        {
        }

        internal PackageAction(PackageActionType actionType, PackageIdentity packageName, JObject package, InstallationTarget target, IPackage corePackage)
        {
            ActionType = actionType;
            PackageIdentity = packageName;
            Package = package;
            Target = target;
            CorePackage = corePackage;
        }

        public override string ToString()
        {
            return 
                ActionType.ToString() + " " + PackageIdentity.ToString() + 
                (Target == null ? String.Empty : (" " + Target.Name));
        }
    }

    public enum PackageActionType
    {
        /// <summary>
        /// The package is to be installed into a project.
        /// </summary>
        Install,

        /// <summary>
        /// The package is to be uninstalled from a project.
        /// </summary>
        Uninstall,

        /// <summary>
        /// The package is to be purged from the packages folder for the solution.
        /// </summary>
        Purge,

        /// <summary>
        /// The package is to be downloaded to the packages folder for the solution.
        /// </summary>
        Download
    }
}
