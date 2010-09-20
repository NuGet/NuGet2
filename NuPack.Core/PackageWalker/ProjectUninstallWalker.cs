namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal class ProjectUninstallWalker : UninstallWalker {        
        public ProjectUninstallWalker(IPackageRepository repository, ILogger listener)
            : base(repository, listener) {
        }

        protected override void WarnRemovingPackageBreaksDependents(IPackage package, IEnumerable<IPackage> dependents) {
            Listener.Log(MessageLevel.Warning, NuPackResources.Warning_RemovingPackageReferenceWillBreakDependents, package.GetFullName(), String.Join(", ", dependents.Select(d => d.GetFullName())));
        }

        protected override InvalidOperationException CreatePackageHasDependentsException(IPackage package, IEnumerable<IPackage> dependents) {
            if (dependents.Count() == 1) {
                return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                       NuPackResources.PackageHasDependentReference, package.GetFullName(), dependents.Single().GetFullName()));
            }

            return new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        NuPackResources.PackageHasMultipleDependentsReferenced, package.GetFullName(), String.Join(", ",
                        dependents.Select(d => d.GetFullName()))));
        }        
    }
}
