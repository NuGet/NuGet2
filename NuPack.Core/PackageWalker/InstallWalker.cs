namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuGet.Resources;

    public class InstallWalker : PackageWalker, IPackageOperationResolver {
        private bool _ignoreDependencies;
        public InstallWalker(IPackageRepository localRepository,
                             IPackageRepository sourceRepository,
                             ILogger logger,
                             bool ignoreDependencies) {

            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            if (localRepository == null) {
                throw new ArgumentNullException("localRepository");
            }
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }

            Repository = localRepository;
            Logger = logger;
            SourceRepository = sourceRepository;
            _ignoreDependencies = ignoreDependencies;
            Operations = new List<PackageOperation>();
        }

        protected ILogger Logger {
            get;
            private set;
        }

        protected IPackageRepository Repository {
            get;
            private set;
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

        protected IList<PackageOperation> Operations {
            get;
            private set;
        }

        protected override void OnAfterDependencyWalk(IPackage package) {
            Operations.Add(new PackageOperation(package, PackageAction.Install));
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            // See if we have a local copy
            IPackage package = Repository.FindPackage(dependency);

            if (package != null) {
                // We have it installed locally
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_DependencyAlreadyInstalled, dependency);
            }
            else {
                // We didn't resolve the dependency so try to retrieve it from the source
                Logger.Log(MessageLevel.Info, NuGetResources.Log_AttemptingToRetrievePackageFromSource, dependency);

                package = SourceRepository.FindPackage(dependency);

                if (package != null) {
                    Logger.Log(MessageLevel.Info, NuGetResources.Log_PackageRetrieveSuccessfully);
                }
            }

            return package;
        }
        
        protected override void OnDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuGetResources.UnableToResolveDependency, dependency));
        }

        protected override bool OnBeforeResolveDependency(PackageDependency dependency) {
            // REVIEW: Efficiency?
            IEnumerable<IPackage> packages = from p in Marker.Packages
                                             where p.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)
                                             select p;

            IPackage package = packages.FindByVersion(dependency.MinVersion, dependency.MaxVersion, dependency.Version);

            // Return false if this package is already resolved (i.e. it's been visited)
            return package == null || !Marker.IsVisited(package);
        }

        public IEnumerable<PackageOperation> ResolveOperations(IPackage package) {
            Operations.Clear();
            Walk(package);
            return Operations;
        }
    }
}
