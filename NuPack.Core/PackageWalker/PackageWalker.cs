namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using NuGet.Resources;

    public abstract class PackageWalker {
        private readonly Dictionary<IPackage, PackageWalkInfo> _packageLookup = new Dictionary<IPackage, PackageWalkInfo>();
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

                    // Set the parent
                    PackageWalkInfo dependencyInfo = GetPackageInfo(resolvedDependency);
                    dependencyInfo.Parent = package;

                    if (!OnAfterResolveDependency(package, resolvedDependency)) {
                        continue;
                    }

                    if (Marker.IsCycle(resolvedDependency)) {
                        if (RaiseErrorOnCycle) {
                            List<IPackage> packages = Marker.Packages.ToList();
                            packages.Add(resolvedDependency);

                            throw new InvalidOperationException(
                               String.Format(CultureInfo.CurrentCulture,
                               NuGetResources.CircularDependencyDetected, String.Join(" => ",
                               packages.Select(p => p.GetFullName()))));
                        }

                        continue;
                    }

                    Walk(resolvedDependency);
                }
            }

            // Mark the package as visited
            Marker.MarkVisited(package);

            PackageWalkInfo info = GetPackageInfo(package);

            // If our parent is an unknown then we need to bubble up the type
            if (info.Parent != null) {
                PackageWalkInfo parentInfo = GetPackageInfo(info.Parent);

                Debug.Assert(parentInfo != null);

                if (parentInfo.InitialTarget == PackageTargets.None) {
                    // Update the parent target type
                    parentInfo.Target |= info.Target;

                    // If we ended up with both that means we found a dependency only packges
                    // that has a mix of solution and project level packages
                    if (parentInfo.Target == PackageTargets.Both) {
                        throw new InvalidOperationException(NuGetResources.DependencyOnlyCannotMixDependencies);
                    }
                }

                // Solution packages can't depend on project level packages
                if (parentInfo.Target == PackageTargets.External && info.Target.HasFlag(PackageTargets.Project)) {
                    throw new InvalidOperationException(NuGetResources.ExternalPackagesCannotDependOnProjectLevelPackages);
                }
            }

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

        protected internal PackageWalkInfo GetPackageInfo(IPackage package) {
            PackageWalkInfo info;
            if (!_packageLookup.TryGetValue(package, out info)) {
                info = new PackageWalkInfo(GetPackageTarget(package));
                _packageLookup.Add(package, info);
            }
            return info;
        }

        private static PackageTargets GetPackageTarget(IPackage package) {
            if (package.HasProjectContent()) {
                return PackageTargets.Project;
            }

            if (package.IsDependencyOnly()) {
                return PackageTargets.None;
            }

            return PackageTargets.External;
        }
    }
}
