using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageRepository))]
    public class VsPackageSourceRepository : IServiceBasedRepository, ICloneableRepository, IPackageLookup, ILatestPackageLookup, IOperationAwareRepository
    {
        private readonly IVsPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        [ImportingConstructor]
        public VsPackageSourceRepository(IPackageRepositoryFactory repositoryFactory,
                                         IVsPackageSourceProvider packageSourceProvider)
        {
            if (repositoryFactory == null)
            {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public string Source
        {
            get
            {
                var activeRepository = GetActiveRepository();
                return activeRepository == null ? null : activeRepository.Source;
            }
        }

        public PackageSaveModes PackageSaveMode
        {
            set { throw new NotSupportedException(); }
            get { throw new NotSupportedException(); }
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                var activeRepository = GetActiveRepository();
                return activeRepository != null && activeRepository.SupportsPrereleasePackages;
            }
        }

        public IQueryable<IPackage> GetPackages()
        {
            var activeRepository = GetActiveRepository();
            return activeRepository == null ? Enumerable.Empty<IPackage>().AsQueryable() : activeRepository.GetPackages();
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            var activeRepository = GetActiveRepository();
            return activeRepository == null ? null : activeRepository.FindPackage(packageId, version);
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            var activeRepository = GetActiveRepository();
            return activeRepository != null ? activeRepository.Exists(packageId, version) : false;
        }

        public void AddPackage(IPackage package)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                throw new InvalidOperationException(VsResources.NoActivePackageSource);
            }
            
            activeRepository.AddPackage(package);
        }

        public void RemovePackage(IPackage package)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                throw new InvalidOperationException(VsResources.NoActivePackageSource);
            }
            
            activeRepository.RemovePackage(package);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions, bool includeDelisted)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                return Enumerable.Empty<IPackage>().AsQueryable();
            }
            
            return activeRepository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions, includeDelisted);
        }

        public IPackageRepository Clone()
        {
            var activeRepository = GetActiveRepository();
            
            return activeRepository == null ? this : activeRepository.Clone();
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                return Enumerable.Empty<IPackage>();
            }

            return activeRepository.FindPackagesById(packageId);
        }

        public IEnumerable<IPackage> GetUpdates(
            IEnumerable<IPackageName> packages, 
            bool includePrerelease, 
            bool includeAllVersions, 
            IEnumerable<FrameworkName> targetFrameworks,
            IEnumerable<IVersionSpec> versionConstraints)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                return Enumerable.Empty<IPackage>();
            }

            return activeRepository.GetUpdates(packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints);
        }

        public bool TryFindLatestPackageById(string id, out SemanticVersion latestVersion)
        {
            var latestPackageLookup = GetActiveRepository() as ILatestPackageLookup;
            if (latestPackageLookup != null)
            {
                return latestPackageLookup.TryFindLatestPackageById(id, out latestVersion);
            }

            latestVersion = null;
            return false;
        }

        public bool TryFindLatestPackageById(string id, bool includePrerelease, out IPackage package)
        {
            var latestPackageLookup = GetActiveRepository() as ILatestPackageLookup;
            if (latestPackageLookup != null)
            {
                return latestPackageLookup.TryFindLatestPackageById(id, includePrerelease, out package);
            }

            package = null;
            return false;
        }

        internal IPackageRepository GetActiveRepository()
        {
            if (_packageSourceProvider.ActivePackageSource == null)
            {
                return null;
            }
            return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
        }

        public IDisposable StartOperation(string operation, string mainPackageId, string mainPackageVersion)
        {
            var activeRepository = GetActiveRepository();
            return activeRepository.StartOperation(operation, mainPackageId, mainPackageVersion);
        }
    }
}
