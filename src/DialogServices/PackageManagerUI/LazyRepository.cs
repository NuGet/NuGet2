using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet.Dialog.Providers
{
    public class LazyRepository : IServiceBasedRepository, IOperationAwareRepository, ILatestPackageLookup, IPackageLookup
    {
        private readonly Lazy<IPackageRepository> _repository;

        private IPackageRepository Repository
        {
            get
            {
                return _repository.Value;
            }
        }

        public string Source
        {
            get
            {
                return Repository.Source;
            }
        }

        public PackageSaveModes PackageSaveMode
        {
            get
            {
                return Repository.PackageSaveMode;
            }
            set
            {
                Repository.PackageSaveMode = value;
            }
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return Repository.SupportsPrereleasePackages;
            }
        }

        public LazyRepository(IPackageRepositoryFactory factory, PackageSource source)
        {
            _repository = new Lazy<IPackageRepository>(() => factory.CreateRepository(source.Source));
        }

        public IQueryable<IPackage> GetPackages()
        {
            return Repository.GetPackages();
        }

        public void AddPackage(IPackage package)
        {
            Repository.AddPackage(package);
        }

        public void RemovePackage(IPackage package)
        {
            Repository.RemovePackage(package);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions, bool includeDelisted)
        {
            return Repository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions, includeDelisted);
        }

        public IEnumerable<IPackage> GetUpdates(
            IEnumerable<IPackageName> 
            packages, 
            bool includePrerelease, 
            bool includeAllVersions, 
            IEnumerable<FrameworkName> targetFrameworks,
            IEnumerable<IVersionSpec> versionConstraints)
        {
            return Repository.GetUpdates(packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints);
        }

        public bool TryFindLatestPackageById(string id, out SemanticVersion latestVersion)
        {
            var latestPackageLookup = Repository as ILatestPackageLookup;
            if (latestPackageLookup != null)
            {
                return latestPackageLookup.TryFindLatestPackageById(id, out latestVersion);
            }

            latestVersion = null;
            return false;
        }

        public bool TryFindLatestPackageById(string id, bool includePrerelease, out IPackage package)
        {
            var latestPackageLookup = Repository as ILatestPackageLookup;
            if (latestPackageLookup != null)
            {
                return latestPackageLookup.TryFindLatestPackageById(id, includePrerelease, out package);
            }

            package = null;
            return false;
        }

        public IDisposable StartOperation(string operation, string mainPackageId, string mainPackageVersion)
        {
            // Starting an operation is an action that should materialize the repository
            return Repository.StartOperation(operation, mainPackageId, mainPackageVersion);
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            return Repository.Exists(packageId, version);
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return Repository.FindPackage(packageId, version);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return Repository.FindPackagesById(packageId);
        }
    }
}