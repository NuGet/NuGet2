namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    public class UninstallWalker : PackageWalker, IPackageOperationResolver {
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
            RemoveDependencies = removeDependencies;
            Force = forceRemove;
            SkippedPackages = new Dictionary<IPackage, IEnumerable<IPackage>>(PackageComparer.IdAndVersionComparer);
            Operations = new Stack<PackageOperation>();
            LogWarnings = true;
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
                return !RemoveDependencies;
            }
        }

        private Stack<PackageOperation> Operations {
            get;
            set;
        }

        public bool RemoveDependencies {
            get;
            private set;
        }

        public bool Force {
            get;
            private set;
        }

        public bool LogWarnings {
            get;
            set;
        }

        private IDictionary<IPackage, IEnumerable<IPackage>> SkippedPackages {
            get;
            set;
        }

        protected IDependentsResolver DependentsResolver {
            get;
            private set;
        }

        protected override void OnBeforeDependencyWalk(IPackage package) {
            // Before choosing to uninstall a package we need to figure out if it is in use
            IEnumerable<IPackage> dependents = GetDependents(package);
            if (dependents.Any()) {
                if (Force) {
                    // We're going to uninstall this package even though other packages depend on it
                    // Warn the user of the other packages that might break as a result and continue processing the package
                    SkippedPackages[package] = dependents;
                }
                else {
                    // We're not ignoring dependents so raise an error telling the user what the dependents are
                    throw CreatePackageHasDependentsException(package, dependents);
                }
            }
        }

        protected override void OnAfterDependencyWalk(IPackage package) {
            Operations.Push(new PackageOperation(package, PackageAction.Uninstall));
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected virtual void WarnRemovingPackageBreaksDependents(IPackage package, IEnumerable<IPackage> dependents) {
            Logger.Log(MessageLevel.Warning, NuPackResources.Warning_UninstallingPackageWillBreakDependents, package.GetFullName(), String.Join(", ", dependents.Select(d => d.GetFullName())));
        }

        protected virtual InvalidOperationException CreatePackageHasDependentsException(IPackage package, IEnumerable<IPackage> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuPackResources.PackageHasDependent, package.GetFullName(), dependents.Single().GetFullName()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.PackageHasDependents, package.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.GetFullName()))));

        }

        protected override void OnDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                NuPackResources.UnableToLocateDependency,
                                dependency));
        }

        public IEnumerable<PackageOperation> ResolveOperations(IPackage package) {
            Walk(package);

            if (LogWarnings) {
                var packages = new HashSet<IPackage>(from o in Operations
                                                     select o.Package, PackageComparer.IdAndVersionComparer);

                foreach (var pair in SkippedPackages) {
                    if (!packages.Contains(pair.Key)) {
                        Logger.Log(MessageLevel.Warning, NuPackResources.Warning_UninstallingPackageWillBreakDependents,
                                   pair.Key,
                                   String.Join(", ", pair.Value.Select(p => p.GetFullName())));
                    }
                }
            }

            return Operations;
        }

        private IEnumerable<IPackage> GetDependents(IPackage package) {
            // REVIEW: Perf?
            return from p in DependentsResolver.GetDependents(package)
                   where !IsConnected(p)
                   select p;
        }

        private bool IsConnected(IPackage package) {
            // We could cache the results of this lookup
            if (Marker.Packages.Contains(package, PackageComparer.IdAndVersionComparer)) {
                return true;
            }

            IEnumerable<IPackage> dependents = DependentsResolver.GetDependents(package);
            return dependents.Any() && dependents.All(IsConnected);
        }
    }
}
