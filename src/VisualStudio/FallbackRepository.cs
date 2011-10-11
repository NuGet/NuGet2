using System.Collections.Generic;
using System.Linq;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Represents a package repository that implements a dependency provider. 
    /// </summary>
    public class FallbackRepository : IPackageRepository, IDependencyResolver, ISearchableRepository
    {
        private readonly IPackageRepository _primaryRepository;
        private readonly IPackageRepository _dependencyResolver;

        public FallbackRepository(IPackageRepository primaryRepository, IPackageRepository dependencyResolver)
        {
            _primaryRepository = primaryRepository;
            _dependencyResolver = dependencyResolver;
        }

        public string Source
        {
            get { return _primaryRepository.Source; }
        }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return _primaryRepository.SupportsPrereleasePackages;
            }
        }

        internal IPackageRepository SourceRepository
        {
            get { return _primaryRepository; }
        }

        internal IPackageRepository DependencyResolver
        {
            get { return _dependencyResolver; }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return _primaryRepository.GetPackages();
        }

        public void AddPackage(IPackage package)
        {
            _primaryRepository.AddPackage(package);
        }

        public void RemovePackage(IPackage package)
        {
            _primaryRepository.RemovePackage(package);
        }

        public IPackage ResolveDependency(PackageDependency dependency, IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions)
        {
            // Use the primary repository to look up dependencies. Fallback to the aggregate repository only if we can't find a package here.
            return _primaryRepository.ResolveDependency(dependency, constraintProvider, allowPrereleaseVersions) ??
                   _dependencyResolver.ResolveDependency(dependency, constraintProvider, allowPrereleaseVersions);
        }

        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            return _primaryRepository.Search(searchTerm, targetFrameworks, allowPrereleaseVersions);
        }
    }
}
