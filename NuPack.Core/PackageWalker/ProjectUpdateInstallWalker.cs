namespace NuPack {
    using System.Collections.Generic;
    using System.Linq;

    internal class ProjectUpdateInstallWalker : ProjectInstallWalker {
        public ProjectUpdateInstallWalker(IEnumerable<IPackage> dependentsToExclude,
                                          IPackageRepository localRepository,
                                          IPackageRepository sourceRepository,
                                          PackageEventListener listener,
                                          bool ignoreDependencies)
            : base(localRepository, sourceRepository, listener, ignoreDependencies) {
            DependentsToExclude = dependentsToExclude;
        }

        private IEnumerable<IPackage> DependentsToExclude { get; set; }

        protected override IEnumerable<IPackage> GetDependents(IPackage package) {
            return base.GetDependents(package)
                       .Except(DependentsToExclude, PackageComparer.IdAndVersionComparer);
        }

        protected override void BeforeWalk(IPackage package) {
            IPackage installedPackage = Repository.FindPackage(package.Id);
            if (installedPackage != null) {
                CheckConflict(package, installedPackage);
            }
            base.BeforeWalk(package);
        }
    }
}