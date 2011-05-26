using System;
using System.IO;

[assembly: CLSCompliant(true)]

namespace NuGet.Common {
    public interface IGalleryServer {
        void CreatePackage(string apiKey, Stream stream);
        void CreatePackage(string apiKey, string externalUrl);
        void PublishPackage(string apiKey, string packageId, string packageVersion);
        void DeletePackage(string apiKey, string packageID, string packageVersion);
        void RatePackage(string packageID, string packageVersion, string rating);
    }
}