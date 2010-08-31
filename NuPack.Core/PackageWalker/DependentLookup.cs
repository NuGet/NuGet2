namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    internal class DependentLookup {
        IDictionary<Package, HashSet<Package>> _dependentsLookup;

        public DependentLookup(IDictionary<Package, HashSet<Package>> dependentsLookup) {
            _dependentsLookup = dependentsLookup;
        }

        public static DependentLookup Create(IPackageRepository repository) {
            var walker = new ReverseDependencyWalker(repository);
            foreach (Package package in repository.GetPackages()) {
                walker.Walk(package);
            }
            return new DependentLookup(walker.Dependents);
        }

        public IEnumerable<Package> GetDependents(Package package) {
            HashSet<Package> dependents;
            if (_dependentsLookup.TryGetValue(package, out dependents)) {
                return dependents;
            }
            return Enumerable.Empty<Package>();
        }
    }
}
