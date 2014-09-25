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
        /// Gets a boolean indicating if the installation target is active and available for installation (i.e. is it open).
        /// </summary>
        public abstract bool IsActive { get; }

        /// <summary>
        /// Gets a boolean indicating if the installation target is a solution target.
        /// </summary>
        public virtual bool IsSolution
        {
            get { return TargetProjects.Count() > 1; }
        }

        /// <summary>
        /// Gets a list of installed packages in all projects in the solution, including those NOT targetted by this installation target.
        /// </summary>
        public abstract IEnumerable<InstalledPackagesList> InstalledPackagesInAllProjects { get; }

        /// <summary>
        /// Gets a list of all projects targetted by this installation target.
        /// </summary>
        public abstract IEnumerable<TargetProject> TargetProjects { get; }

        /// <summary>
        /// Searches the installed packages list
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public abstract Task<IEnumerable<JObject>> SearchInstalled(string searchTerm, int skip, int take, CancellationToken cancelToken);

        /// <summary>
        /// Gets the project with the specified name, if it exists, otherwise returns null.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public virtual TargetProject GetProject(string projectName)
        {
            return TargetProjects.FirstOrDefault(p => String.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
