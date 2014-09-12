using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using NewPackageAction = NuGet.Client.Resolution.PackageAction;

namespace NuGet.Client
{
    /// <summary>
    /// Represents a target into which packages can be installed
    /// </summary>
    public abstract class InstallationTarget
    {
        /// <summary>
        /// Gets the name of the target in which packages will be installed (for example, the Project name when targetting a Project)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a list of packages installed in the target. This is basically an interface to packages.config.
        /// </summary>
        public abstract InstalledPackagesList Installed { get; }

        #region Ugly stuff that needs to be reviewed and reorganized
        public abstract Task<IEnumerable<InstalledPackagesList>> GetInstalledPackagesInAllProjects();
        public abstract IProjectSystem ProjectSystem { get; }
        #endregion

        /// <summary>
        /// Gets the list of frameworks supported by this target.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();

        /// <summary>
        /// Executes the specified action against this installation target.
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public abstract Task ExecuteActionsAsync(IEnumerable<NewPackageAction> actions);
    }
}
