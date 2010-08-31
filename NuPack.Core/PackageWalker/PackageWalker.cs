namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class PackageWalker {
        private PackageMarker _marker;
        public PackageWalker() {
            
        }

        protected virtual bool RaiseErrorOnCycle {
            get {
                return true;
            }
        }

        protected virtual bool IgnoreDependencies {
            get {
                return false;
            }
        }

        protected PackageMarker Marker {
            get {
                if (_marker == null) {
                    _marker = CreateMarker();
                }
                return _marker;
            }
        }

        protected virtual PackageMarker CreateMarker() {
            return new PackageMarker();
        }

        public void Walk(Package package) {
            BeforeWalk(package);
            WalkInternal(package);
        }

        private void WalkInternal(Package package) {
            // Do nothing if we saw this package already
            if (Marker.IsVisited(package)) {
                return;
            }

            // Mark the package as processing
            Marker.MarkProcessing(package);

            if (!IgnoreDependencies) {
                foreach (var dependency in package.Dependencies) {
                    if (SkipDependency(dependency)) {
                        // Skip dependency before we resolve it
                        continue;
                    }

                    // Resolve the dependency
                    Package resolvedDependency = ResolveDependency(dependency);

                    if (resolvedDependency == null) {
                        RaiseDependencyResolveError(dependency);
                        return;
                    }

                    if (SkipResolvedDependency(package, dependency, resolvedDependency)) {
                        // Skip dependency after resolution
                        continue;
                    }

                    ProcessResolvedDependency(package, dependency, resolvedDependency);                    

                    if (Marker.IsCycle(resolvedDependency)) {
                        if (RaiseErrorOnCycle) {
                            List<Package> packages = Marker.Packages.ToList();
                            packages.Add(resolvedDependency);
                            RaiseCycleError(packages);
                            return;
                        }
                        continue;
                    }

                    WalkInternal(resolvedDependency);
                }
            }

            // Mark the package as visited
            Marker.MarkVisited(package);

            Process(package);
        }

        protected virtual bool SkipResolvedDependency(Package package, PackageDependency dependency, Package resolvedDependency) {
            return false;
        }

        protected virtual void RaiseCycleError(IEnumerable<Package> packages) {
        }

        protected virtual bool SkipDependency(PackageDependency dependency) {
            return false;
        }

        protected virtual void ProcessResolvedDependency(Package package, PackageDependency dependency, Package resolvedDependency) {
        }

        protected virtual void Process(Package package) {
        }

        protected virtual void RaiseDependencyResolveError(PackageDependency dependency) {
        }

        protected abstract Package ResolveDependency(PackageDependency dependency);

        protected virtual void BeforeWalk(Package package) {
        }
    }
}