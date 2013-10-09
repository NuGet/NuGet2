using System;
using System.IO;

namespace NuGet
{
    public interface IPackageCacheRepository : IPackageRepository
    {
        bool InvokeOnPackage(string packageId, SemanticVersion version, Action<Stream> action);
    }
}