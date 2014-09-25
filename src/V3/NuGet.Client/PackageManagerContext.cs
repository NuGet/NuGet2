using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public abstract class PackageManagerContext
    {
        /// <summary>
        /// Gets the source manager used to manage active and available package repositories
        /// </summary>
        public abstract SourceRepositoryManager SourceManager { get; }

        /// <summary>
        /// Gets the names of all the projects in the context.
        /// </summary>
        public abstract IEnumerable<string> ProjectNames { get; }

        /// <summary>
        /// Creates an Installation Target to install packages into the specified target project.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public abstract ProjectInstallationTarget CreateProjectInstallationTarget(string projectName);

        /// <summary>
        /// Creates an Installation Target to install packages into the current solution.
        /// </summary>
        /// <returns></returns>
        public abstract InstallationTarget CreateSolutionInstallationTarget();
    }
}
