namespace NuPack {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using NuPack.Resources;

    internal abstract class BasicPackageWalker : PackageWalker {
        public BasicPackageWalker(IPackageRepository repository, PackageEventListener listener) {
            Repository = repository;
            Listener = listener;
        }

        protected PackageEventListener Listener { get; private set; }

        protected IPackageRepository Repository { get; private set; }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return Repository.FindPackage(dependency.Id, dependency.MinVersion, dependency.MaxVersion, dependency.Version);
        }

        protected override void RaiseDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuPackResources.UnableToResolveDependency, dependency));
        }

        protected override void RaiseCycleError(IEnumerable<IPackage> packages) {
            throw new InvalidOperationException(
                                String.Format(CultureInfo.CurrentCulture,
                                NuPackResources.CircularDependencyDetected, String.Join(" => ",
                                packages.Select(p => p.GetFullName()))));
        }
    }
}