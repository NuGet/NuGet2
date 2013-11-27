using System;
using System.Linq;
using System.Collections.Generic;
using NuGet;

namespace NuGet.WebMatrix
{
    internal interface INuGetPackageManager
    {
        IEnumerable<IPackage> FindDependenciesToBeInstalled(IPackage package);

        IEnumerable<IPackage> GetPackagesToBeInstalledForUpdateAll();

        IPackage FindPackage(string packageId, SemanticVersion version);

        IEnumerable<IPackage> FindPackages(IEnumerable<string> packageIds);

        IQueryable<IPackage> GetInstalledPackages();

        IEnumerable<IPackage> GetPackagesWithUpdates();

        IQueryable<IPackage> GetRemotePackages();

        IEnumerable<string> InstallPackage(IPackage package);

        bool IsPackageInstalled(IPackage package);

        IQueryable<IPackage> SearchRemotePackages(string filterString);

        IEnumerable<string> UninstallPackage(IPackage package);

        IEnumerable<string> UpdatePackage(IPackage package);

        IEnumerable<string> UpdateAllPackages();

        IPackage GetUpdate(IPackage package);

        bool SupportsEnableDisable
        {
            get;
        }

        bool IsPackageEnabled(IPackage package);

        void TogglePackageEnabled(IPackage package);

        bool IsMandatory(IPackage package);

        bool IncludePrerelease
        {
            get;
            set;
        }
    }
}
