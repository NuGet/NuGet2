using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    public interface ISearchableRepository
    {
        IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions);
    }
}
