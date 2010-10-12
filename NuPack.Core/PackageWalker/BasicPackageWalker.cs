namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    public abstract class BasicPackageWalker : PackageWalker {
        protected BasicPackageWalker(IPackageRepository repository, ILogger logger) {
            if (repository == null) {
                throw new ArgumentNullException("repository");
            }
            if (logger == null) {
                throw new ArgumentNullException("logger");
            }
            Repository = repository;
            Logger = logger;
        }

        protected ILogger Logger {
            get;
            private set;
        }

        protected IPackageRepository Repository {
            get;
            private set;
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override void OnDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuPackResources.UnableToResolveDependency, dependency));
        }

        protected override void OnCycleError(IEnumerable<IPackage> packages) {
            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                NuPackResources.CircularDependencyDetected, String.Join(" => ",
                                packages.Select(p => p.GetFullName()))));
        }
    }
}