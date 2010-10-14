namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    public class UninstallWalker : BasicPackageWalker, IDependencyResolver {
        private Stack<IPackage> _packages;

        public UninstallWalker(IPackageRepository repository,
                               IDependencyResolver dependentsResolver,
                               ILogger logger,
                               bool removeDependencies,
                               bool forceRemove)
            : base(repository, logger) {
            if (dependentsResolver == null) {
                throw new ArgumentNullException("dependentsResolver");
            }
            DependentsResolver = dependentsResolver;
            RemoveDependencies = removeDependencies;
            Force = forceRemove;
            SkippedPackages = new Dictionary<IPackage, IEnumerable<IPackage>>(PackageComparer.IdAndVersionComparer);
        }

        protected override bool IgnoreDependencies {
            get {
                return !RemoveDependencies;
            }
        }

        public Stack<IPackage> Packages {
            get {
                if (_packages == null) {
                    _packages = new Stack<IPackage>();
                }
                return _packages;
            }
        }

        public bool RemoveDependencies {
            get;
            private set;
        }

        public bool Force {
            get;
            private set;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "It's not worth it creating a type for this")]
        public IDictionary<IPackage, IEnumerable<IPackage>> SkippedPackages {
            get;
            private set;
        }

        protected IDependencyResolver DependentsResolver {
            get;
            private set;
        }

        protected override void BeforeWalk(IPackage package) {
            // Before choosing to uninstall a package we need to figure out if it is in use
            IEnumerable<IPackage> dependents = DependentsResolver.ResolveDependencies(package);
            if (dependents.Any()) {
                if (Force) {
                    // We're going to uninstall this package even though other packages depend on it
                    // Warn the user of the other packages that might break as a result and continue processing the package
                    WarnRemovingPackageBreaksDependents(package, dependents);
                }
                else {
                    // We're not ignoring dependents so raise an error telling the user what the dependents are
                    throw CreatePackageHasDependentsException(package, dependents);
                }
            }

            base.BeforeWalk(package);
        }

        protected override void Process(IPackage package) {
            Packages.Push(package);
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override bool SkipResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            IEnumerable<IPackage> dependents = DependentsResolver.ResolveDependencies(resolvedDependency)
                                                                 .Except(Marker.Packages, PackageComparer.IdAndVersionComparer);
            if (!Force && dependents.Any()) {
                // If we aren't ignoring dependents then we skip this dependency if it has any dependents
                SkippedPackages[resolvedDependency] = dependents;
                return true;
            }
            return false;
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

        public IEnumerable<IPackage> ResolveDependencies(IPackage package) {
            Walk(package);
            LogSkippedPackages();
            return Packages;
        }

        protected virtual void LogSkippedPackages() {
            foreach (var pair in SkippedPackages) {
                if (!Packages.Contains(pair.Key, PackageComparer.IdAndVersionComparer)) {
                    Logger.Log(MessageLevel.Warning, NuPackResources.Warning_PackageSkippedBecauseItIsInUse,
                                pair.Key,
                                String.Join(", ", pair.Value.Select(p => p.GetFullName())));
                }
            }
        }
    }
}
