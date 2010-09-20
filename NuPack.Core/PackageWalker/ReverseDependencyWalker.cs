namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal class ReverseDependencyWalker : PackageWalker {
        public ReverseDependencyWalker(IPackageRepository repository) {
            Repository = repository;
            Dependents = new Dictionary<IPackage, HashSet<IPackage>>(PackageComparer.IdAndVersionComparer);
        }

        protected override bool RaiseErrorOnCycle {
            get {
                return false;
            }
        }

        private IPackageRepository Repository { get; set; }

        public IDictionary<IPackage, HashSet<IPackage>> Dependents { get; set; }

        protected override PackageMarker CreateMarker() {
            return new PackageMarker(PackageComparer.IdAndVersionComparer);
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override void ProcessResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            HashSet<IPackage> values;
            if (!Dependents.TryGetValue(resolvedDependency, out values)) {
                values = new HashSet<IPackage>(PackageComparer.IdAndVersionComparer);
                Dependents[resolvedDependency] = values;
            }

            // Add the current package to the list of dependents
            values.Add(package);
        }
    }
}