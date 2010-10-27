using System;
using System.Collections.Concurrent;

namespace NuGet.VisualStudio {
    public class CachedRepositoryFactory : IPackageRepositoryFactory {
        private static readonly IPackageRepositoryFactory _instance = new CachedRepositoryFactory(PackageRepositoryFactory.Default);

        private readonly ConcurrentDictionary<string, IPackageRepository> _repositoryCache = new ConcurrentDictionary<string, IPackageRepository>(StringComparer.OrdinalIgnoreCase);
        private readonly IPackageRepositoryFactory _repositoryFactory;

        public static IPackageRepositoryFactory Instance {
            get {
                return _instance;
            }
        }

        public CachedRepositoryFactory(IPackageRepositoryFactory repositoryFactory) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            _repositoryFactory = repositoryFactory;
        }

        public IPackageRepository CreateRepository(string source) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source, out repository)) {
                repository = _repositoryFactory.CreateRepository(source);
                _repositoryCache.TryAdd(source, repository);
            }
            return repository;
        }
    }
}
