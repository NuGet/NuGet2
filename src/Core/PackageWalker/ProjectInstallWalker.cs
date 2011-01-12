namespace NuGet {
    public class ProjectInstallWalker : InstallWalker {
        private readonly IDependentsResolver _dependentsResolver;

        public ProjectInstallWalker(IPackageRepository localRepository,
                                    IPackageRepository sourceRepository,
                                    IDependentsResolver dependentsResolver,
                                    ILogger logger,
                                    bool ignoreDependencies)
            : base(localRepository, sourceRepository, logger, ignoreDependencies) {
            _dependentsResolver = dependentsResolver;
        }

        protected override ConflictResult GetConflict(string packageId) {
            // For project installs we first try to base behavior (using the live graph)
            // then we look for conflicts for packages installed into the current project.
            ConflictResult result = base.GetConflict(packageId);

            if (result == null) {
                IPackage package = Repository.FindPackage(packageId);
                if (package != null) {
                    result = new ConflictResult(package, Repository, _dependentsResolver);
                }
            }
            return result;
        }

        protected override void OnAfterPackageWalk(IPackage package) {
            PackageWalkInfo info = GetPackageInfo(package);

            if (info.Target == PackageTargets.Project) {
                base.OnAfterPackageWalk(package);
            }
        }
    }
}
