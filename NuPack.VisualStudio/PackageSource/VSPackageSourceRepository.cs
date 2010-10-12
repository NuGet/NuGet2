using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NuPack.VisualStudio {
    public class VSPackageSourceRepository : IPackageRepository {
        private readonly VSPackageSourceProvider _packageSourceProvider;
        private readonly IPackageRepositoryFactory _repositoryFactory;

        private static readonly ConcurrentDictionary<string, IPackageRepository> _repositoryCache = new ConcurrentDictionary<string, IPackageRepository>(StringComparer.OrdinalIgnoreCase);

        public VSPackageSourceRepository(IPackageRepositoryFactory repositoryFactory, VSPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }

            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        private IPackageRepository ActiveRepository {
            get {
                return GetRepository(_packageSourceProvider.ActivePackageSource);
            }
        }

        public IQueryable<IPackage> GetPackages() {
            return ActiveRepository.GetPackages();
        }

        public IPackage FindPackage(string packageId, Version version) {
            return ActiveRepository.FindPackage(packageId, version);
        }

        public void AddPackage(IPackage package) {
            ActiveRepository.AddPackage(package);
        }

        public void RemovePackage(IPackage package) {
            ActiveRepository.RemovePackage(package);
        }

        private IPackageRepository GetRepository(PackageSource source) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source.Source, out repository)) {
                repository = _repositoryFactory.CreateRepository(source.Source);
                _repositoryCache.TryAdd(source.Source, repository);
            }
            return repository;
        }
    }
}
