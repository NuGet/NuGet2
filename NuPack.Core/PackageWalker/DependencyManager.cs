namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NuPack.Resources;

    internal static class DependencyManager {
        internal static IEnumerable<Package> ResolveDependenciesForInstall(Package package,
                                                                            IPackageRepository localRepository,
                                                                            IPackageRepository sourceRepository,
                                                                            PackageEventListener listener = null) {
            listener = listener ?? PackageEventListener.Default;
            var walker = new InstallWalker(localRepository,
                                           sourceRepository,
                                           listener);

            walker.Walk(package);

            return walker.Output;
        }

        internal static IEnumerable<Package> ResolveDependenciesForProjectInstall(Package package,
                                                                                   IPackageRepository localRepository,
                                                                                   IPackageRepository sourceRepository,
                                                                                   PackageEventListener listener = null) {
            listener = listener ?? PackageEventListener.Default;
            var walker = new ProjectInstallWalker(localRepository, sourceRepository, listener);

            walker.Walk(package);
            return walker.Output;
        }

        internal static IEnumerable<Package> ResolveDependenciesForProjectUninstall(Package package,
                                                                                    IPackageRepository localRepository,
                                                                                    bool ignoreDepents = false,
                                                                                    bool removeDependencies = false,
                                                                                    PackageEventListener listener = null) {
            listener = listener ?? PackageEventListener.Default;
            var walker = new ProjectUninstallWalker(localRepository, listener);
            walker.Force = ignoreDepents;
            walker.RemoveDependencies = removeDependencies;

            walker.Walk(package);

            LogSkippedPackages(listener, walker);

            return walker.Output;
        }

        internal static IEnumerable<Package> ResolveDependenciesForUninstall(Package package,
                                                                             IPackageRepository localRepository,
                                                                             bool force = false,
                                                                             bool removeDependencies = false,
                                                                             PackageEventListener listener = null) {
            listener = listener ?? PackageEventListener.Default;
            var walker = new UninstallWalker(localRepository, listener);
            walker.Force = force;
            walker.RemoveDependencies = removeDependencies;

            walker.Walk(package);

            LogSkippedPackages(listener, walker);

            return walker.Output;
        }

        private static void LogSkippedPackages(PackageEventListener listener, UninstallWalker walker) {
            foreach (var pair in walker.SkippedPackages) {
                if (!walker.Output.Contains(pair.Key, PackageComparer.IdAndVersionComparer)) {
                    listener.OnReportStatus(StatusLevel.Warning, NuPackResources.Warning_PackageSkippedBecauseItIsInUse,
                                pair.Key,
                                String.Join(", ", pair.Value.Select(p => p.ToString())));
                }
            }
        }

        internal static PackagePlan ResolveDependenciesForUpdate(Package oldPackage,
                                                                 Package newPackage,
                                                                 IPackageRepository localRepository,
                                                                 IPackageRepository sourceRepository,
                                                                 PackageEventListener listener,
                                                                 bool updateDependencies) {
            listener = listener ?? PackageEventListener.Default;
            var uninstallWalker = new ProjectUpdateUninstallWalker(localRepository, listener);
            uninstallWalker.RemoveDependencies = updateDependencies;

            uninstallWalker.Walk(oldPackage);

            var installWalker = new ProjectUpdateInstallWalker(uninstallWalker.Output,
                                                               localRepository,
                                                               sourceRepository,
                                                               listener,
                                                               !updateDependencies);

            installWalker.Walk(newPackage);

            return new PackagePlan(installWalker.Output, uninstallWalker.Output);
        }
    }
}