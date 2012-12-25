using System.IO;

namespace NuGet.Test.Mocks
{
    public class MockPackageCacheRepository : MockPackageRepository, IPackageCacheRepository
    {
        public Stream CreatePackageStream(string packageId, SemanticVersion version)
        {
            return new MemoryStream();
        }
    }
}
