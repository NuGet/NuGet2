namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal class UninstallWalker : BasicPackageWalker {
        private DependentLookup _dependentsLookup;

        public UninstallWalker(IPackageRepository repository, PackageEventListener listener)
            : base(repository, listener) {
            SkippedPackages = new Dictionary<Package, IEnumerable<Package>>(PackageComparer.IdAndVersionComparer);
        }

        protected override bool IgnoreDependencies {
            get {
                return !RemoveDependencies;
            }
        }

        public IList<Package> Output { get; set; }

        public bool RemoveDependencies { get; set; }

        public bool Force { get; set; }

        public IDictionary<Package, IEnumerable<Package>> SkippedPackages { get; set; }

        private DependentLookup DependentsLookup {
            get {
                if (_dependentsLookup == null) {
                    _dependentsLookup = DependentLookup.Create(Repository);
                }
                return _dependentsLookup;
            }
        }

        protected override void BeforeWalk(Package package) {
            // Before choosing to uninstall a package we need to figure out if it is in use
            IEnumerable<Package> dependents = DependentsLookup.GetDependents(package);
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
        
        protected override void Process(Package package) {
            if (Output == null) {
                Output = new List<Package>();
            }
            Output.Add(package);
        }

        protected override Package ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override bool SkipResolvedDependency(Package package, PackageDependency dependency, Package resolvedDependency) {
            IEnumerable<Package> dependents = DependentsLookup.GetDependents(resolvedDependency)
                                                               .Except(Marker.Packages, PackageComparer.IdAndVersionComparer);
            if (!Force && dependents.Any()) {
                // If we aren't ignoring dependents then we skip this dependency if it has any dependents
                SkippedPackages[resolvedDependency] = dependents;
                return true;
            }
            return false;
        }

        protected virtual void WarnRemovingPackageBreaksDependents(Package package, IEnumerable<Package> dependents) {
            Listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_UninstallingPackageWillBreakDependents, package, String.Join(", ", dependents.Select(d => d.ToString())));
        }

        protected virtual InvalidOperationException CreatePackageHasDependentsException(Package package, IEnumerable<Package> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuPackResources.PackageHasDependent, package, dependents.Single()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.PackageHasDependents, package, String.Join(", ",
                        dependents.Select(d => d.ToString()))));

        }

        protected override void RaiseDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                NuPackResources.UnableToLocateDependency,
                                dependency));
        }
    }
}
