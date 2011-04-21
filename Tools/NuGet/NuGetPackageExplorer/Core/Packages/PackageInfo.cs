using System;
using System.Data.Services.Common;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [HasStreamAttribute]
    public class PackageInfo {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Authors { get; set; }
        public double VersionRating { get; set; }
        public int VersionDownloadCount { get; set; }
        public string PackageHash { get; set; }
        public Uri DownloadUrl { get; set; }

        public DataServicePackage AsDataServicePackage() {
            return new DataServicePackage {
                Id = Id,
                Version = Version,
                Authors = Authors,
                VersionRating = VersionRating,
                VersionDownloadCount = VersionDownloadCount,
                PackageHash = PackageHash
            };
        }
    }
}