namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal class ProjectUninstallWalker : UninstallWalker {        
        public ProjectUninstallWalker(IPackageRepository repository, PackageEventListener listener)
            : base(repository, listener) {
        }

        protected override void WarnRemovingPackageBreaksDependents(Package package, IEnumerable<Package> dependents) {
            Listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_RemovingPackageReferenceWillBreakDependents, package, String.Join(", ", dependents.Select(d => d.ToString())));
        }

        protected override InvalidOperationException CreatePackageHasDependentsException(Package package, IEnumerable<Package> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuPackResources.PackageHasDependentReference, package, dependents.Single()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.PackageHasMultipleDependentsReferenced, package, String.Join(", ",
                        dependents.Select(d => d.ToString()))));
        }        
    }
}
