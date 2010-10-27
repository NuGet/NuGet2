namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class DependentsWalker : PackageWalker, IDependentsResolver {
        public DependentsWalker(IPackageRepository repository) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }
            Repository = repository;
        }

        protected override bool RaiseErrorOnCycle {
            get {
                return false;
            }
        }

        protected IPackageRepository Repository {
            get;
            private set;
        }

        private IDictionary<IPackage, HashSet<IPackage>> DependentsLookup {
            get;
            set;
        }

        protected override PackageMarker CreateMarker() {
            return new PackageMarker(PackageComparer.IdAndVersionComparer);
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency);
        }

        protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            Debug.Assert(DependentsLookup != null);

            HashSet<IPackage> values;
            if (!DependentsLookup.TryGetValue(dependency, out values)) {
                values = new HashSet<IPackage>(PackageComparer.IdAndVersionComparer);
                DependentsLookup[dependency] = values;
            }

            // Add the current package to the list of dependents
            values.Add(package);
            return base.OnAfterResolveDependency(package, dependency);
        }


        public IEnumerable<IPackage> GetDependents(IPackage package) {
            if (DependentsLookup == null) {
                DependentsLookup = new Dictionary<IPackage, HashSet<IPackage>>(PackageComparer.IdAndVersionComparer);
                foreach (IPackage p in Repository.GetPackages()) {
                    Walk(p);
                }
            }

            HashSet<IPackage> dependents;
            if (DependentsLookup.TryGetValue(package, out dependents)) {
                return dependents;
            }
            return Enumerable.Empty<IPackage>();
        }
    }
}
