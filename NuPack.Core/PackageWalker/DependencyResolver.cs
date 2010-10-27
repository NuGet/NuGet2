using System;
using System.Collections.Generic;
using System.Globalization;
using NuGet.Resources;

namespace NuGet {
    public sealed class DependencyResolver : PackageWalker {
        private HashSet<IPackage> _dependencies;
        private readonly IPackageRepository _sourceRepository;

        public DependencyResolver(IPackageRepository sourceRepository) {
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            _sourceRepository = sourceRepository;
        }

        public IEnumerable<IPackage> GetDependencies(IPackage package) {
            _dependencies = new HashSet<IPackage>();
            _dependencies.Add(package);

            Walk(package);
            
            return _dependencies;
        }

        protected override void OnDependencyResolveError(PackageDependency dependency) {
            throw new InvalidOperationException(
                String.Format(CultureInfo.CurrentCulture,
                NuGetResources.UnableToResolveDependency, dependency));
        }

        protected override IPackage ResolveDependency(PackageDependency dependency) {
            return _sourceRepository.FindPackage(dependency);
        }

        protected override bool OnAfterResolveDependency(IPackage package, IPackage dependency) {
            _dependencies.Add(dependency);
            return base.OnAfterResolveDependency(package, dependency);
        }
    }
}
