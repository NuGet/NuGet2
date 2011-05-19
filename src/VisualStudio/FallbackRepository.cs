using System.Linq;

namespace NuGet.VisualStudio {
    /// <summary>
    /// Represents a package repository that implements a dependency provider. 
    /// Dependencies are provided by the fallbackRepository.
    /// </summary>
    public class FallbackRepository : IPackageRepository, IDependencyProvider {
        private readonly IPackageRepository _primaryRepository;
        private readonly IPackageRepository _fallbackRepository;

        public FallbackRepository(IPackageRepository primaryRepository, IPackageRepository fallbackRepository) {
            _primaryRepository = primaryRepository;
            _fallbackRepository = fallbackRepository;
        }

        public string Source {
            get { return _primaryRepository.Source; }
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

        public IQueryable<IPackage> GetDependencies(string packageId) {
            return _fallbackRepository.FindPackagesById(packageId);
        }
    }
}
