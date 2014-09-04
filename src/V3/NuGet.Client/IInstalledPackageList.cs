using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client
{
    public interface IInstalledPackageList
    {
        /// <summary>
        /// Creates an IPackageSearcher for searching locally installed packages
        /// </summary>
        /// <returns></returns>
        IPackageSearcher CreateSearcher();

        /// <summary>
        /// Retrieves a list of installed packages
        /// </summary>
        /// <returns></returns>
        IEnumerable<PackageName> GetInstalledPackages();

        /// <summary>
        /// Retrieves either a) null if the specified package is not installed or b) the version that is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        SemanticVersion GetInstalledVersion(string packageId);
        
        /// <summary>
        /// Returns a boolean indicating if a package with the specific ID/Version pair is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="packageVersion"></param>
        /// <returns></returns>
        bool IsInstalled(string packageId, SemanticVersion packageVersion);
    }
}
