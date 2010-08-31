namespace NuPack {
    internal class ProjectUpdateUninstallWalker : UninstallWalker {
        public ProjectUpdateUninstallWalker(IPackageRepository repository, PackageEventListener listener)
            : base(repository, listener) {
        }

        protected override bool SkipResolvedDependency(Package package, PackageDependency dependency, Package resolvedDependency) {
            return false;
        }

        protected override void BeforeWalk(Package package) {
        }
    }
}