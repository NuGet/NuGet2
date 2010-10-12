namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DependentsResolver : IDependencyResolver {
        private readonly IPackageRepository _repository;
        private IDictionary<IPackage, HashSet<IPackage>> _dependentsLookup;

        public DependentsResolver(IPackageRepository repository) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }
            _repository = repository;
        }

        public IEnumerable<IPackage> ResolveDependencies(IPackage package) {
            EnsureLookup();
            HashSet<IPackage> dependents;
            if (_dependentsLookup.TryGetValue(package, out dependents)) {
                return dependents;
            }
            return Enumerable.Empty<IPackage>();
        }

        private void EnsureLookup() {
            if (_dependentsLookup == null) {
                var walker = new ReverseDependencyWalker(_repository);
                foreach (IPackage package in _repository.GetPackages()) {
                    walker.Walk(package);
                }
                _dependentsLookup = walker.Dependents;
            }
        }
    }
}
