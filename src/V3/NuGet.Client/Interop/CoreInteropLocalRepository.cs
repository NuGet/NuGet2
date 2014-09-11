using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Interop
{
    /// <summary>
    /// IPackageRepository designed to wrap the InstalledPackagesList up for NuGet.Core-interop.
    /// </summary>
    internal class CoreInteropLocalRepository : IPackageRepository
    {
        private readonly InstalledPackagesList _installed;

        public CoreInteropLocalRepository(InstalledPackagesList installed)
        {
            _installed = installed;
        }

        public IQueryable<IPackage> GetPackages()
        {
            return _installed.GetAllInstalledPackagesAndMetadata().Result
                .Select(j => new CoreInteropPackage(j))
                .AsQueryable();
        }

        #region Unimplemented Stuff
        public PackageSaveModes PackageSaveMode
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

        public bool SupportsPrereleasePackages
        {
            get { throw new NotImplementedException(); }
        }

        public string Source
        {
            get { throw new NotImplementedException(); }
        }

        public void AddPackage(IPackage package)
        {
            throw new NotImplementedException();
        }

        public void RemovePackage(IPackage package)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    /// <summary>
    /// ISharedPackageRepository designed to wrap the InstallationTarget up for NuGet.Core-interop.
    /// </summary>
    internal class CoreInteropSharedRepository : CoreInteropLocalRepository, ISharedPackageRepository
    {
        private readonly InstallationTarget _target;
        
        public CoreInteropSharedRepository(InstallationTarget target) :
            base(target.Installed)
        {
            _target = target;
        }

        public bool IsReferenced(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException();
        }

        public bool IsSolutionReferenced(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException();
        }

        public void RegisterRepository(PackageReferenceFile packageReferenceFile)
        {
            throw new NotImplementedException();
        }

        public void UnregisterRepository(PackageReferenceFile packageReferenceFile)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPackageRepository> LoadProjectRepositories()
        {
            return _target.GetInstalledPackagesInAllProjects().Result
                .Select(p => new CoreInteropLocalRepository(p));
        }
    }
}
