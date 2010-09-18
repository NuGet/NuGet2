namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    internal class DependentLookup {
        IDictionary<IPackage, HashSet<IPackage>> _dependentsLookup;

        public DependentLookup(IDictionary<IPackage, HashSet<IPackage>> dependentsLookup) {
            _dependentsLookup = dependentsLookup;
        }

        public static DependentLookup Create(IPackageRepository repository) {
            var walker = new ReverseDependencyWalker(repository);
            foreach (IPackage package in repository.GetPackages()) {
                walker.Walk(package);
            }
            return new DependentLookup(walker.Dependents);
        }

        public IEnumerable<IPackage> GetDependents(IPackage package) {
            HashSet<IPackage> dependents;
            if (_dependentsLookup.TryGetValue(package, out dependents)) {
                return dependents;
            }
            return Enumerable.Empty<IPackage>();
        }
    }
}
