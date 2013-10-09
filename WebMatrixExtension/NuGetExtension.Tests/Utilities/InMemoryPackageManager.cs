using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;
using Xunit;

namespace NuGet.WebMatrix.Tests.Utilities
{
    public class InMemoryPackageManager : INuGetPackageManager
    {
        public static readonly InMemoryPackageManager Empty = new InMemoryPackageManager();

        public InMemoryPackageManager()
        {
            this.DisabledPackages = new HashSet<IPackage>(PackageEqualityComparer.Id);
            this.InstalledPackages = new HashSet<IPackage>(PackageEqualityComparer.Id);
            this.RemotePackages = new HashSet<IPackage>(PackageEqualityComparer.IdAndVersion);
        }

        public HashSet<IPackage> DisabledPackages
        {
            get;
            private set;
        }

        public HashSet<IPackage> InstalledPackages
        {
            get;
            private set;
        }

        public HashSet<IPackage> RemotePackages
        {
            get;
            private set;
        }

        public IEnumerable<IPackage> FindDependenciesToBeInstalled(IPackage package)
        {
            var dependencies = package.GetCompatiblePackageDependencies(null);
            return dependencies.SelectMany(dependency => this.RemotePackages.Where(remote => remote.Id == dependency.Id && dependency.VersionSpec.Satisfies(remote.Version)));
        }

        public IEnumerable<IPackage> FindPackages(IEnumerable<string> packageIds)
        {
            return packageIds.SelectMany(id => this.RemotePackages.Where(remote => remote.Id == id));
        }

        public IQueryable<IPackage> GetInstalledPackages()
        {
            return this.InstalledPackages.AsQueryable<IPackage>();
        }

        public IEnumerable<IPackage> GetPackagesWithUpdates()
        {
            return this.RemotePackages.Where(remote => this.InstalledPackages.Any(installed => remote.Id == installed.Id && remote.Version > installed.Version));
        }

        public IQueryable<IPackage> GetRemotePackages()
        {
            return this.RemotePackages.AsQueryable<IPackage>();
        }

        public IEnumerable<string> InstallPackage(IPackage package)
        {
            Assert.False(this.InstalledPackages.Contains(package), "The package must not be already installed");
            this.InstalledPackages.Add(package);
            return Enumerable.Empty<string>();
        }

        public bool IsPackageEnabled(IPackage package)
        {
            Assert.True(this.InstalledPackages.Contains(package), "The package be already installed");
            return !this.DisabledPackages.Contains(package);
        }

        public bool IsPackageInstalled(IPackage package)
        {
            return this.InstalledPackages.Contains(package);
        }

        public IQueryable<IPackage> SearchRemotePackages(string filterString)
        {
            throw new NotImplementedException();
        }

        public bool SupportsEnableDisable
        {
            get;
            set;
        }

        public void TogglePackageEnabled(IPackage package)
        {
            Assert.True(this.SupportsEnableDisable, "The package manager must support enable/disable.");
            Assert.True(this.InstalledPackages.Contains(package), "The package be already installed.");

            if (this.DisabledPackages.Contains(package))
            {
                this.DisabledPackages.Remove(package);
            }
            else
            {
                this.DisabledPackages.Add(package);
            }
        }

        public IEnumerable<string> UninstallPackage(IPackage package)
        {
            Assert.True(this.InstalledPackages.Contains(package), "The package be already installed");

            this.InstalledPackages.Remove(package);
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> UpdatePackage(IPackage package)
        {
            Assert.True(this.InstalledPackages.Contains(package), "The package be already installed");

            this.InstalledPackages.Remove(package);
            this.InstalledPackages.Add(package);
            return Enumerable.Empty<string>();
        }

        private static readonly string[] MandatoryPackages = new string[] { "WebMatrixConfig" };
        public bool IsMandatory(IPackage package)
        {
            return MandatoryPackages.Contains(package.Id);
        }

        public bool IncludePrerelease
        {
            get
            {
                return false;
            }
            set
            {
                // Do Nothing
            }
        }
    }
}
