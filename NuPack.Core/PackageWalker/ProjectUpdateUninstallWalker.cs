namespace NuPack {
    internal class ProjectUpdateUninstallWalker : UninstallWalker {
        public ProjectUpdateUninstallWalker(IPackageRepository repository, IPackageEventListener listener)
            : base(repository, listener) {
        }

        protected override bool SkipResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            return false;
        }

        protected override void BeforeWalk(IPackage package) {
        }
    }
}