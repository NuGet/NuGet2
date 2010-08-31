namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    internal class ProjectUpdateInstallWalker : ProjectInstallWalker {
        public ProjectUpdateInstallWalker(IEnumerable<Package> dependentsToExclude,
                                          IPackageRepository localRepository,
                                          IPackageRepository sourceRepository,
                                          PackageEventListener listener,
                                          bool ignoreDependencies)
            : base(localRepository, sourceRepository, listener, ignoreDependencies) {
            DependentsToExclude = dependentsToExclude;
        }

        private IEnumerable<Package> DependentsToExclude { get; set; }

        protected override IEnumerable<Package> GetDependents(Package package) {
            return base.GetDependents(package)
                       .Except(DependentsToExclude, PackageComparer.IdAndVersionComparer);
        }

        protected override void BeforeWalk(Package package) {
            Package installedPackage = Repository.FindPackage(package.Id);
            if (installedPackage != null) {
                CheckConflict(package, installedPackage);
            }
            base.BeforeWalk(package);
        }
    }
}