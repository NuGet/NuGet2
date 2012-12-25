using System.IO;

namespace NuGet
{
    public interface IPackageCacheRepository : IPackageRepository
    {
        Stream CreatePackageStream(string packageId, SemanticVersion version);
    }
}