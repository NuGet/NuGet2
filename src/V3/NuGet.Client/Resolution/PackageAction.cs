using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Resolution
{
    /// <summary>
    /// Represents an action that needs to be taken on a package.
    /// </summary>
    public class PackageAction
    {
        public PackageIdentity PackageIdentity { get; private set; }
        public JObject Package { get; private set; }
        public TargetProject Target { get; private set; }
        public PackageActionType ActionType { get; private set; }

        public PackageAction(PackageActionType actionType, PackageIdentity packageName, JObject package, TargetProject target)
        {
            ActionType = actionType;
            PackageIdentity = packageName;
            Package = package;
            Target = target;
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
