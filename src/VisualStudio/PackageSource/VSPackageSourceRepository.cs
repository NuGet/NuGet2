using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [Export(typeof(IPackageRepository))]
    public class VsPackageSourceRepository : IPackageRepository, ISearchableRepository, ICloneableRepository, IFindPackagesRepository
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

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            var activeRepository = GetActiveRepository();
            if (activeRepository == null)
            {
                return Enumerable.Empty<IPackage>().AsQueryable();
            }
            return activeRepository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions);
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

        internal IPackageRepository GetActiveRepository()
        {
            if (_packageSourceProvider.ActivePackageSource == null)
            {
                return null;
            }
            return _repositoryFactory.CreateRepository(_packageSourceProvider.ActivePackageSource.Source);
        }
    }
}
