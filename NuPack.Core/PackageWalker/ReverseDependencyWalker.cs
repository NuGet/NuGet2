namespace NuPack {
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System;

    public class ReverseDependencyWalker : PackageWalker {
        public ReverseDependencyWalker(IPackageRepository repository) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }
            Repository = repository;
            Dependents = new Dictionary<IPackage, HashSet<IPackage>>(PackageComparer.IdAndVersionComparer);
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

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "It's not worth it creating a type for this")]
        public IDictionary<IPackage, HashSet<IPackage>> Dependents {
            get;
            private set;
        }

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