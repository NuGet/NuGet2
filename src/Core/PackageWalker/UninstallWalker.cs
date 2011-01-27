using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    public class UninstallWalker : PackageWalker, IPackageOperationResolver {
        private IDictionary<IPackage, IEnumerable<IPackage>> _forcedRemoved = new Dictionary<IPackage, IEnumerable<IPackage>>(PackageEqualityComparer.IdAndVersion);
        private IDictionary<IPackage, IEnumerable<IPackage>> _skippedPackages = new Dictionary<IPackage, IEnumerable<IPackage>>(PackageEqualityComparer.IdAndVersion);
        private bool _removeDependencies;

        public UninstallWalker(IPackageRepository repository,
                               IDependentsResolver dependentsResolver,
                               ILogger logger,
                               bool removeDependencies,
                               bool forceRemove) {
            if (dependentsResolver == null) {
                throw new ArgumentNullException("dependentsResolver");
            }
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }

            Logger = logger;
            Repository = repository;
            DependentsResolver = dependentsResolver;
            Force = forceRemove;
            ThrowOnConflicts = true;
            Operations = new Stack<PackageOperation>();
            _removeDependencies = removeDependencies;
        }

        protected ILogger Logger {
            get;
            private set;
        }

        protected IPackageRepository Repository {
            get;
            private set;
        }

        protected override bool IgnoreDependencies {
            get {
                return !_removeDependencies;
            }
        }

        protected override bool SkipDependencyResolveError {
            get {
                return true;
            }
        }

        private Stack<PackageOperation> Operations {
            get;
            set;
        }

        public bool Force {
            get;
            private set;
        }

        public bool ThrowOnConflicts { get; set; }

        protected IDependentsResolver DependentsResolver {
            get;
            private set;
        }

        protected override void OnBeforePackageWalk(IPackage package) {
            // Before choosing to uninstall a package we need to figure out if it is in use
            IEnumerable<IPackage> dependents = GetDependents(package);
            if (dependents.Any()) {
                if (Force) {
                    // We're going to uninstall this package even though other packages depend on it                    
                    _forcedRemoved[package] = dependents;
                }
                else if (ThrowOnConflicts) {
                    // We're not ignoring dependents so raise an error telling the user what the dependents are
                    throw CreatePackageHasDependentsException(package, dependents);
                }
            }
        }

        protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            if (!Force) {
                IEnumerable<IPackage> dependents = GetDependents(dependency);

                // If this isn't a force remove and other packages depend on this dependency
                // then skip this entire dependency tree
                if (dependents.Any()) {
                    _skippedPackages[dependency] = dependents;

                    // Don't look any further
                    return false;
                }
            }

            return true;
        }

        protected override void OnAfterPackageWalk(IPackage package) {
            Operations.Push(new PackageOperation(package, PackageAction.Uninstall));
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindDependency(dependency);
        }

        protected virtual void WarnRemovingPackageBreaksDependents(IPackage package, IEnumerable<IPackage> dependents) {
            Logger.Log(MessageLevel.Warning, NuGetResources.Warning_UninstallingPackageWillBreakDependents, package.GetFullName(), String.Join(", ", dependents.Select(d => d.GetFullName())));
        }

        protected virtual InvalidOperationException CreatePackageHasDependentsException(IPackage package, IEnumerable<IPackage> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuGetResources.PackageHasDependent, package.GetFullName(), dependents.Single().GetFullName()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.PackageHasDependents, package.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.GetFullName()))));

        }

        protected override void OnDependencyResolveError(PackageDependency dependency) {
            Logger.Log(MessageLevel.Warning, NuGetResources.UnableToLocateDependency, dependency);
        }

        public IEnumerable<PackageOperation> ResolveOperations(IPackage package) {
            Operations.Clear();
            Marker.Clear();

            Walk(package);

            // Log warnings for packages that were forcibly removed
            foreach (var pair in _forcedRemoved) {
                Logger.Log(MessageLevel.Warning, NuGetResources.Warning_UninstallingPackageWillBreakDependents,
                           pair.Key,
                           String.Join(", ", pair.Value.Select(p => p.GetFullName())));
            }

            // Log warnings for dependencies that were skipped
            foreach (var pair in _skippedPackages) {
                Logger.Log(MessageLevel.Warning, NuGetResources.Warning_PackageSkippedBecauseItIsInUse,
                           pair.Key,
                           String.Join(", ", pair.Value.Select(p => p.GetFullName())));
            }

            return Operations.Reduce();
        }

        private IEnumerable<IPackage> GetDependents(IPackage package) {
            // REVIEW: Perf?
            return from p in DependentsResolver.GetDependents(package)
                   where !IsConnected(p)
                   select p;
        }

        private bool IsConnected(IPackage package) {
            // We could cache the results of this lookup
            if (Marker.Contains(package)) {
                return true;
            }

            IEnumerable<IPackage> dependents = DependentsResolver.GetDependents(package);
            return dependents.Any() && dependents.All(IsConnected);
        }
    }
}
