using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio {
    /// <summary>
    /// Represents a package repository that implements a dependency provider. 
    /// </summary>
    public class FallbackRepository : IPackageRepository, IDependencyProvider {
        private readonly IPackageRepository _primaryRepository;
        private readonly IPackageRepository _dependencyResolver;

        public FallbackRepository(IPackageRepository primaryRepository, IPackageRepository dependencyResolver) {
            _primaryRepository = primaryRepository;
            _dependencyResolver = dependencyResolver;
        }

        public string Source {
            get { return _primaryRepository.Source; }
        }

        internal IPackageRepository DependencyResolver {
            get { return _dependencyResolver; }
        }

        public IQueryable<IPackage> GetPackages() {
            return _primaryRepository.GetPackages();
        }

        public void AddPackage(IPackage package) {
            _primaryRepository.AddPackage(package);
        }

        public void RemovePackage(IPackage package) {
            _primaryRepository.RemovePackage(package);
        }

        public IEnumerable<IPackage> GetDependencies(string packageId) {
            return _dependencyResolver.FindPackagesById(packageId);
        }
    }
}
