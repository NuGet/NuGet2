using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using NuGet.Client.Diagnostics;

namespace NuGet.Client.Interop
{
    /// <summary>
    /// IPackageRepository designed to wrap the InstalledPackagesList up for NuGet.Core-interop.
    /// </summary>
    internal class CoreInteropLocalRepository : IPackageRepository
    {
        protected InstalledPackagesList Installed { get; private set; }

        public CoreInteropLocalRepository(InstalledPackagesList installed)
        {
            Installed = installed;
        }

        public IQueryable<IPackage> GetPackages()
        {
            NuGetTraceSources.CoreInterop.Verbose("getinstalledpackages", "Retrieved all installed packages.");
            return Installed.GetAllInstalledPackagesAndMetadata().Result
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
    internal class CoreInteropSharedRepository : CoreInteropLocalRepository, ISharedPackageRepository, IPackageReferenceRepository
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
            NuGetTraceSources.CoreInterop.Verbose("loadprojectrepositories", "Project Repositories Loaded");
            return _target.GetInstalledPackagesInAllProjects().Result
                .Select(p => new CoreInteropLocalRepository(p));
        }

        public void AddPackage(string packageId, SemanticVersion version, bool developmentDependency, System.Runtime.Versioning.FrameworkName targetFramework)
        {
            throw new NotImplementedException();
        }

        public FrameworkName GetPackageTargetFramework(string packageId)
        {
            NuGetTraceSources.CoreInterop.Verbose("getpkgtargetfx", "Get target framework for {0}", packageId);
            var package = Installed.GetInstalledPackage(packageId);
            if (package == null)
            {
                return null;
            }
            return package.TargetFramework;
        }
    }
}
