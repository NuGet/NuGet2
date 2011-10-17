using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Dialog.Providers
{
    public class LazyRepository : IPackageRepository, ISearchableRepository, IFindPackagesRepository
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

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            return Repository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return Repository.FindPackagesById(packageId);
        }
    }
}