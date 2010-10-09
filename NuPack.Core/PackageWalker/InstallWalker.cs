namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NuPack.Resources;

    internal class InstallWalker : BasicPackageWalker {
        private bool _ignoreDependencies;
        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             ILogger listener,
                             bool ignoreDependencies = false) :
            base(localRepository, listener) {

            _ignoreDependencies = ignoreDependencies;
            SourceRepository = sourceRepository;
        }

        protected override bool IgnoreDependencies {
            get {
                return _ignoreDependencies;
            }
        }

        protected IPackageRepository SourceRepository {
            get;
            set;
        }

        // List of packages to install after the walk
        // If this is null, then there were errors
        public IList<IPackage> Output {
            get;
            private set;
        }

        protected override void Process(IPackage package) {
            if (Output == null) {
                Output = new List<IPackage>();
            }

            Output.Add(package);
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
                    Listener.Log(MessageLevel.Info, NuPackResources.Log_PackageRetrieveSuccessfully);
                }
            }

            return package;
        }

        protected virtual void LogDependencyExists(PackageDependency dependency) {
            Listener.Log(MessageLevel.Debug, NuPackResources.Debug_DependencyAlreadyInstalled, dependency);
        }

        protected virtual void LogRetrieveDependencyFromSource(PackageDependency dependency) {
            // We didn't resolve the dependency so try to retrieve it from the source
            Listener.Log(MessageLevel.Info, NuPackResources.Log_AttemptingToRetrievePackageFromSource, dependency);
        }

        protected override bool SkipDependency(PackageDependency dependency) {
            IQueryable<IPackage> packages = (from p in Marker.Packages
                                            where p.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)
                                            select p).AsQueryable();

            IPackage package = packages.FindByVersion(dependency.MinVersion, dependency.MaxVersion, dependency.Version);
            return package != null && Marker.IsVisited(package);
        }
    }
}