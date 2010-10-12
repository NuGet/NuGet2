namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    public abstract class PackageWalker {
        private PackageMarker _marker;
        protected PackageWalker() {           
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

        public void Walk(IPackage package) {
            BeforeWalk(package);
            WalkInternal(package);
        }

        private void WalkInternal(IPackage package) {
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
                    IPackage resolvedDependency = ResolveDependency(dependency);

                    if (resolvedDependency == null) {
                        OnDependencyResolveError(dependency);
                        return;
                    }

                    if (SkipResolvedDependency(package, dependency, resolvedDependency)) {
                        // Skip dependency after resolution
                        continue;
                    }

                    ProcessResolvedDependency(package, dependency, resolvedDependency);                    

                    if (Marker.IsCycle(resolvedDependency)) {
                        if (RaiseErrorOnCycle) {
                            List<IPackage> packages = Marker.Packages.ToList();
                            packages.Add(resolvedDependency);
                            OnCycleError(packages);
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

        protected virtual bool SkipResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            return false;
        }

        protected virtual void OnCycleError(IEnumerable<IPackage> packages) {
        }

        protected virtual bool SkipDependency(PackageDependency dependency) {
            return false;
        }

        protected virtual void ProcessResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
        }

        protected virtual void Process(IPackage package) {
        }

        protected virtual void OnDependencyResolveError(PackageDependency dependency) {
        }

        protected abstract IPackage ResolveDependency(PackageDependency dependency);

        protected virtual void BeforeWalk(IPackage package) {
        }
    }
}