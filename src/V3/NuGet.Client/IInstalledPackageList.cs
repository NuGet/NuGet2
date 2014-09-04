using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Versioning;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerable<PackageIdentity> GetInstalledPackages();

        /// <summary>
        /// Retrieves either a) null if the specified package is not installed or b) the version that is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        NuGetVersion GetInstalledVersion(string packageId);
        
        /// <summary>
        /// Returns a boolean indicating if a package with the specific ID/Version pair is installed.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="packageVersion"></param>
        /// <returns></returns>
        bool IsInstalled(string packageId, NuGetVersion packageVersion);
    }
}
