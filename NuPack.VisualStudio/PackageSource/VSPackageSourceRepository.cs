using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NuPack.VisualStudio {
    public class VSPackageSourceRepository : IPackageRepository {
        private readonly VSPackageSourceProvider _packageSourceProvider;
        private static readonly ConcurrentDictionary<string, IPackageRepository> _repositoryCache = new ConcurrentDictionary<string, IPackageRepository>(StringComparer.OrdinalIgnoreCase);
        public VSPackageSourceRepository(VSPackageSourceProvider packageSourceProvider) {
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
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

        private static IPackageRepository GetRepository(PackageSource source) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source.Source, out repository)) {
                repository = PackageRepositoryFactory.CreateRepository(source.Source);
                _repositoryCache.TryAdd(source.Source, repository);
            }
            return repository;
        }
    }
}
