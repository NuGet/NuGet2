using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public interface IServiceBasedRepository
    {
        IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions);
        IEnumerable<IPackage> FindPackagesById(string packageId);
        IEnumerable<IPackage> GetUpdates(IEnumerable<IPackage> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFramework);
    }
}
