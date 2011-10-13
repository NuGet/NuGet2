using System.IO;

namespace NuGet
{
    public interface IPackageServer
    {
        string Source { get; }

        void CreatePackage(string apiKey, Stream packageStream);
        void PublishPackage(string apiKey, string packageId, string packageVersion);
        void DeletePackage(string apiKey, string packageId, string packageVersion);
    }
}