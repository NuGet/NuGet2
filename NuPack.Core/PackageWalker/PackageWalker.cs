namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

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
            // Do nothing if we saw this package already
            if (Marker.IsVisited(package)) {
                return;
            }

            OnBeforeDependencyWalk(package);

            // Mark the package as processing
            Marker.MarkProcessing(package);

            if (!IgnoreDependencies) {
                foreach (var dependency in package.Dependencies) {
                    if (!OnBeforeResolveDependency(dependency)) {
                        continue;
                    }

                    IPackage resolvedDependency = ResolveDependency(dependency);

                    if (resolvedDependency == null) {
                        OnDependencyResolveError(dependency);
                        return;
                    }

                    if (!OnAfterResolveDependency(package, resolvedDependency)) {
                        continue;
                    }

                    if (Marker.IsCycle(resolvedDependency)) {
                        if (RaiseErrorOnCycle) {
                            List<IPackage> packages = Marker.Packages.ToList();
                            packages.Add(resolvedDependency);

                            throw new InvalidOperationException(
                               String.Format(CultureInfo.CurrentCulture,
                               NuPackResources.CircularDependencyDetected, String.Join(" => ",
                               packages.Select(p => p.GetFullName()))));
                        }

                        continue;
                    }

                    Walk(resolvedDependency);
                }
            }

            // Mark the package as visited
            Marker.MarkVisited(package);

            OnAfterDependencyWalk(package);
        }

        protected virtual bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            return true;
        }

        protected virtual bool OnBeforeResolveDependency(PackageDependency dependency) {
            return true;
        }

        protected virtual void OnBeforeDependencyWalk(IPackage package) {
        }

        protected virtual void OnAfterDependencyWalk(IPackage package) {
        }

        protected virtual void OnDependencyResolveError(PackageDependency dependency) {
        }

        protected abstract IPackage ResolveDependency(PackageDependency dependency);
    }
}