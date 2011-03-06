using System.Linq;
using NuGet.Server.DataServices;

namespace NuGet.Server.Infrastructure {
    public interface IServerPackageRepository : IPackageRepository {
        IQueryable<Package> GetPackagesWithDerivedData();

    }
}
