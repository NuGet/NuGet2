namespace NuPack {
    public class ProjectUpdateUninstallWalker : UninstallWalker {
        public ProjectUpdateUninstallWalker(IPackageRepository repository, 
                                            IDependencyResolver dependentsResolver, 
                                            ILogger logger,
                                            bool removeDependencies,
                                            bool forceRemove)
            : base(repository, 
                   dependentsResolver, 
                   logger,
                   removeDependencies,
                   forceRemove) {
        }

        protected override bool SkipResolvedDependency(IPackage package, PackageDependency dependency, IPackage resolvedDependency) {
            return false;
        }

        protected override void BeforeWalk(IPackage package) {
        }
    }
}