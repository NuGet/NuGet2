
namespace NuGet {
    public class UpdateWalker : InstallWalker {
        private readonly IDependentsResolver _dependentsResolver;

        public UpdateWalker(IPackageRepository localRepository,
                            IPackageRepository sourceRepository,
                            IDependentsResolver dependentsResolver,
                            ILogger logger,
                            bool updateDependencies)
            : base(localRepository,
                   sourceRepository,
                   logger: logger,
                   ignoreDependencies: !updateDependencies) {
            _dependentsResolver = dependentsResolver;
            AcceptedTargets = PackageTargets.All;
        }

        public UpdateWalker(IPackageRepository localRepository,
                            IPackageRepository sourceRepository,
                            IDependentsResolver dependentsResolver,
                            IPackageConstraintProvider constraintProvider,
                            ILogger logger,
                            bool updateDependencies)
            : base(localRepository, sourceRepository, constraintProvider, logger, !updateDependencies) {
            _dependentsResolver = dependentsResolver;
            AcceptedTargets = PackageTargets.All;
        }

        public PackageTargets AcceptedTargets { get; set; }

        protected override ConflictResult GetConflict(IPackage package) {
            // For project installs we first try to base behavior (using the live graph)
            // then we look for conflicts for packages installed into the current project.
            ConflictResult result = base.GetConflict(package);

            if (result == null) {
                IPackage existingPackage = Repository.FindPackage(package.Id);

                if (existingPackage != null) {
                    result = new ConflictResult(existingPackage, Repository, _dependentsResolver);
                }
            }
            return result;
        }

        protected override void OnAfterPackageWalk(IPackage package) {
            PackageWalkInfo info = GetPackageInfo(package);

            if (AcceptedTargets.HasFlag(info.Target)) {
                base.OnAfterPackageWalk(package);
            }
        }
    }
}
