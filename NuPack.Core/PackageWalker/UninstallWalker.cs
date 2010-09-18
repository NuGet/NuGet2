namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal class UninstallWalker : BasicPackageWalker {
        private DependentLookup _dependentsLookup;

        public UninstallWalker(IPackageRepository repository, IPackageEventListener listener)
            : base(repository, listener) {
            SkippedPackages = new Dictionary<IPackage, IEnumerable<IPackage>>(PackageComparer.IdAndVersionComparer);
        }

        protected override bool IgnoreDependencies {
            get {
                return !RemoveDependencies;
            }
        }

        public IList<IPackage> Output { get; set; }

        public bool RemoveDependencies { get; set; }

        public bool Force { get; set; }

        public IDictionary<IPackage, IEnumerable<IPackage>> SkippedPackages { get; set; }

        private DependentLookup DependentsLookup {
            get {
                if (_dependentsLookup == null) {
                    _dependentsLookup = DependentLookup.Create(Repository);
                }
                return _dependentsLookup;
            }
        }

        protected override void BeforeWalk(IPackage package) {
            // Before choosing to uninstall a package we need to figure out if it is in use
            IEnumerable<IPackage> dependents = DependentsLookup.GetDependents(package);
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
            if (Output == null) {
                Output = new List<IPackage>();
            }
            Output.Add(package);
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override bool SkipResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            IEnumerable<IPackage> dependents = DependentsLookup.GetDependents(resolvedDependency)
                                                               .Except(Marker.Packages, PackageComparer.IdAndVersionComparer);
            if (!Force && dependents.Any()) {
                // If we aren't ignoring dependents then we skip this dependency if it has any dependents
                SkippedPackages[resolvedDependency] = dependents;
                return true;
            }
            return false;
        }

        protected virtual void WarnRemovingPackageBreaksDependents(IPackage package, IEnumerable<IPackage> dependents) {
            Listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_UninstallingPackageWillBreakDependents, package.GetFullName(), String.Join(", ", dependents.Select(d => d.GetFullName())));
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

        protected override void RaiseDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                NuPackResources.UnableToLocateDependency,
                                dependency));
        }
    }
}
