using System.IO;

namespace NuGet
{
    public interface IPackageServer
    {
        string Source { get; }

        void PushPackage(string apiKey, Stream packageStream);
        void DeletePackage(string apiKey, string packageId, string packageVersion);
    }
}