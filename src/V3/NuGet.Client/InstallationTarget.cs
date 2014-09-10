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
        /// Searches the list of installed packages
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="cancelToken"></param>
        /// <returns>Returns a list of JSON objects suitable for rendering by the Package Manager Dialog</returns>
        public abstract Task<IEnumerable<JObject>> SearchInstalledPackages(string searchTerm, int skip, int take, CancellationToken cancelToken);

        /// <summary>
        /// Retrieves a list of installed packages
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public abstract IEnumerable<PackageIdentity> GetInstalledPackages();

        /// <summary>
        /// Retrieves either a) null if the specified package is not installed or b) the version that is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        public abstract NuGetVersion GetInstalledVersion(string packageId);

        /// <summary>
        /// Returns a boolean indicating if a package with the specific ID/Version pair is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="packageVersion"></param>
        /// <returns></returns>
        public abstract bool IsInstalled(string packageId, NuGetVersion packageVersion);

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
