using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace NuGet.WebMatrix.DependentTests
{
    internal class IPackageManagerMock : INuGetPackageManager
    {
        internal Func<IPackage, IEnumerable<IPackage>> FindDependenciesToBeInstalledFunc { get; set; }

        public IEnumerable<IPackage> FindDependenciesToBeInstalled(IPackage package)
        {
            if (FindDependenciesToBeInstalledFunc != null)
            {
                return FindDependenciesToBeInstalledFunc(package);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IEnumerable<string>, IEnumerable<IPackage>> FindPackagesFunc { get; set; }
        
        public IEnumerable<IPackage> FindPackages(IEnumerable<string> packageIds)
        {
            if (FindPackagesFunc != null)
            {
                return FindPackagesFunc(packageIds);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IQueryable<IPackage>> GetInstalledPackagesFunc { get; set; }
        
        public IQueryable<IPackage> GetInstalledPackages()
        {
            if (GetInstalledPackagesFunc != null)
            {
                return GetInstalledPackagesFunc();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IEnumerable<IPackage>> GetPackagesWithUpdatesFunc { get; set; }
        
        public IEnumerable<IPackage> GetPackagesWithUpdates()
        {
            if (GetPackagesWithUpdatesFunc != null)
            {
                return GetPackagesWithUpdatesFunc();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IQueryable<IPackage>> GetRemotePackagesFunc { get; set; }
       
        public IQueryable<IPackage> GetRemotePackages()
        {
            if (GetRemotePackagesFunc != null)
            {
                return GetRemotePackagesFunc();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IPackage, IEnumerable<string>> InstallPackageFunc { get; set; }
        
        public IEnumerable<string> InstallPackage(IPackage package)
        {
            if (InstallPackageFunc != null)
            {
                return InstallPackageFunc(package);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<PackageDependency, bool> IsDependencyInstalledFunc { get; set; }
        
        public bool IsDependencyInstalled(PackageDependency packageDep)
        {
            if (IsDependencyInstalledFunc != null)
            {
                return IsDependencyInstalledFunc(packageDep);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IPackage, bool> IsPackageInstalledFunc { get; set; }
        
        public bool IsPackageInstalled(IPackage package)
        {
            if (IsPackageInstalledFunc != null)
            {
                return IsPackageInstalledFunc(package);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IPackage, IEnumerable<string>> UninstallPackageFunc { get; set; }
        
        public IEnumerable<string> UninstallPackage(IPackage package)
        {
            if (UninstallPackageFunc != null)
            {
                return UninstallPackageFunc(package);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<IPackage, IEnumerable<string>> UpdatePackageFunc { get; set; }
        
        public IEnumerable<string> UpdatePackage(IPackage package)
        {
            if (UpdatePackageFunc != null)
            {
                return UpdatePackageFunc(package);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal Func<string, IQueryable<IPackage>> SearchRemovePackagesFunc { get; set; }

        public IQueryable<IPackage> SearchRemotePackages(string searchText)
        {
            if (SearchRemovePackagesFunc != null)
            {
                return SearchRemovePackagesFunc(searchText);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool IsPackageEnabled(IPackage package)
        {
            throw new NotImplementedException();
        }

        public bool SupportsEnableDisable
        {
            get { throw new NotImplementedException(); }
        }

        public void TogglePackageEnabled(IPackage package)
        {
            throw new NotImplementedException();
        }

        public bool IsMandatory(IPackage package)
        {
            throw new NotImplementedException();
        }


        public bool IncludePrerelease
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
