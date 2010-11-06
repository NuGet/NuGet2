using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPackageRepositoryFactory))]
    public class CachedRepositoryFactory : IPackageRepositoryFactory { 
        private readonly ConcurrentDictionary<PackageSource, IPackageRepository> _repositoryCache = new ConcurrentDictionary<PackageSource, IPackageRepository>();
        private readonly IPackageRepositoryFactory _repositoryFactory;
        
        [ImportingConstructor]
        public CachedRepositoryFactory(VsPackageRepositoryFactory repositoryFactory) {
            if (repositoryFactory == null) {
                throw new ArgumentNullException("repositoryFactory");
            }
            _repositoryFactory = repositoryFactory;
        }

        public IPackageRepository CreateRepository(PackageSource source) {
            IPackageRepository repository;
            if (!_repositoryCache.TryGetValue(source, out repository)) {
                repository = _repositoryFactory.CreateRepository(source);
                _repositoryCache.TryAdd(source, repository);
            }
            return repository;
        }
    }
}
