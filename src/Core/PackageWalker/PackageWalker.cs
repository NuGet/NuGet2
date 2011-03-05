using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    public abstract class PackageWalker {
        private readonly Dictionary<IPackage, PackageWalkInfo> _packageLookup = new Dictionary<IPackage, PackageWalkInfo>();

        protected PackageWalker() {
            Marker = new PackageMarker();
        }

        protected virtual bool RaiseErrorOnCycle {
            get {
                return true;
            }
        }

        protected virtual bool SkipDependencyResolveError {
            get {
                return false;
            }
        }

        protected virtual bool IgnoreDependencies {
            get {
                return false;
            }
        }

        protected PackageMarker Marker {
            get;
            private set;
        }

        protected virtual bool IgnoreWalkInfo {
            get {
                return false;
            }
        }

        public void Walk(IPackage package) {
            // Do nothing if we saw this package already
            if (Marker.IsVisited(package)) {
                return;
            }

            OnBeforePackageWalk(package);

            // Mark the package as processing
            Marker.MarkProcessing(package);

            if (!IgnoreDependencies) {
                foreach (var dependency in package.Dependencies) {
                    // Try to resolve the dependency from the visited packages first
                    IPackage resolvedDependency = Marker.FindDependency(dependency) ??
                                                  ResolveDependency(dependency);

                    if (resolvedDependency == null) {
                        OnDependencyResolveError(dependency);
                        // If we're skipping dependency resolve errros then move on to the next
                        // dependency
                        if (SkipDependencyResolveError) {
                            continue;
                        }

                        return;
                    }

                    if (!IgnoreWalkInfo) {
                        // Set the parent
                        PackageWalkInfo dependencyInfo = GetPackageInfo(resolvedDependency);
                        dependencyInfo.Parent = package;
                    }

                    Marker.AddDependent(package, resolvedDependency);

                    if (!OnAfterResolveDependency(package, resolvedDependency)) {
                        continue;
                    }

                    if (Marker.IsCycle(resolvedDependency) ||
                        Marker.IsVersionCycle(resolvedDependency.Id)) {
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

            if (!IgnoreWalkInfo) {
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
            }

            OnAfterPackageWalk(package);
        }

        protected virtual bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            return true;
        }

        protected virtual void OnBeforePackageWalk(IPackage package) {
        }

        protected virtual void OnAfterPackageWalk(IPackage package) {
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
