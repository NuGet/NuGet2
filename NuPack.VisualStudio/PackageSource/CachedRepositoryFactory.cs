using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NuGet.VisualStudio {
    public class CachedRepositoryFactory : IPackageRepositoryFactory {
        private static readonly IPackageRepositoryFactory _instance = 
            new CachedRepositoryFactory(PackageRepositoryFactory.Default, VsPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE));

        private readonly ConcurrentDictionary<PackageSource, IPackageRepository> _repositoryCache = new ConcurrentDictionary<PackageSource, IPackageRepository>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;

        public static IPackageRepositoryFactory Instance {
            get {
                return _instance;
            }
        }

        public CachedRepositoryFactory(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider packageSourceProvider) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            if (packageSourceProvider == null) {
                throw new ArgumentNullException("packageSourceProvider");
            }
            _repositoryFactory = repositoryFactory;
            _packageSourceProvider = packageSourceProvider;
        }

        public IPackageRepository CreateRepository(PackageSource source) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source, out repository)) {
                if (source.IsAggregate) {
                    repository = new AggregateRepository(_packageSourceProvider.GetPackageSources().Select(_repositoryFactory.CreateRepository));
                }
                else {
                    repository = _repositoryFactory.CreateRepository(source);
                }
                _repositoryCache.TryAdd(source, repository);
            }
            return repository;
        }
    }
}
