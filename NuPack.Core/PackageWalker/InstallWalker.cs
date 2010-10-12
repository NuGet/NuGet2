namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NuPack.Resources;

    public class InstallWalker : BasicPackageWalker, IDependencyResolver {
        private bool _ignoreDependencies;
        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             ILogger logger,
                             bool ignoreDependencies) :
            base(localRepository, logger) {

            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            SourceRepository = sourceRepository;
            _ignoreDependencies = ignoreDependencies;
        }

        protected override bool IgnoreDependencies {
            get {
                return _ignoreDependencies;
            }
        }

        protected IPackageRepository SourceRepository {
            get;
            private set;
        }

        // List of packages to install after the walk
        // If this is null, then there were errors
        public IList<IPackage> Packages {
            get;
            private set;
        }

        protected override void Process(IPackage package) {
            if (Packages == null) {
                Packages = new List<IPackage>();
            }

            Packages.Add(package);
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            // See if we have a local copy
            IPackage package = base.ResolveDependency(dependency);

            if (package != null) {
                // We have it installed locally
                LogDependencyExists(dependency);
            }
            else {
                // We didn't resolve the dependency so try to retrieve it from the source
                LogRetrieveDependencyFromSource(dependency);

                package = SourceRepository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);

                if (package != null) {
                    Logger.Log(MessageLevel.Info, NuPackResources.Log_PackageRetrieveSuccessfully);
                }
            }

            return package;
        }

        protected virtual void LogDependencyExists(PackageDependency dependency) {
            Logger.Log(MessageLevel.Debug, NuPackResources.Debug_DependencyAlreadyInstalled, dependency);
        }

        protected virtual void LogRetrieveDependencyFromSource(PackageDependency dependency) {
            // We didn't resolve the dependency so try to retrieve it from the source
            Logger.Log(MessageLevel.Info, NuPackResources.Log_AttemptingToRetrievePackageFromSource, dependency);
        }

        protected override bool SkipDependency(PackageDependency dependency) {
            IQueryable<IPackage> packages = (from p in Marker.Packages
                                             where p.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)
                                             select p).AsQueryable();

            IPackage package = packages.FindByVersion(dependency.MinVersion, dependency.MaxVersion, dependency.Version);
            return package != null && Marker.IsVisited(package);
        }

        public IEnumerable<IPackage> ResolveDependencies(IPackage package) {
            Walk(package);
            return Packages;
        }
    }
}