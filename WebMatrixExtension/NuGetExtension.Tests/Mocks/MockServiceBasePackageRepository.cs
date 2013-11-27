using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;
using System.Runtime.Versioning;

namespace NuGet.WebMatrix.DependentTests
{
    public class MockServiceBasePackageRepository : MockPackageRepository, IServiceBasedRepository
    {
        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
        {
            if (searchTerm == null)
            {
                var targetFrameworkNames = targetFrameworks.Select(f => new FrameworkName(f));
                return GetPackages().Where(p => SupportsTargetFrameworks(targetFrameworkNames, p));
            }

            throw new NotImplementedException();
        }

        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<System.Runtime.Versioning.FrameworkName> targetFrameworks, IEnumerable<IVersionSpec> versionConstraints)
        {
            var compatiblePackages = packages.SelectMany(p => Packages[p.Id].Where(p2 => p2.Version > p.Version && SupportsTargetFrameworks(targetFrameworks, p2)));
            return compatiblePackages.OrderByDescending(p => p.Version).Distinct(PackageEqualityComparer.Id);
        }

        private static bool SupportsTargetFrameworks(IEnumerable<FrameworkName> targetFramework, IPackage package)
        {
            return targetFramework.IsEmpty() || targetFramework.Any(t => VersionUtility.IsCompatible(t, package.GetSupportedFrameworks()));
        }
    }
}