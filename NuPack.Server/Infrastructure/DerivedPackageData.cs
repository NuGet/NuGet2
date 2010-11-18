using System;

namespace NuGet.Server.Infrastructure {
    public class DerivedPackageData {
        public long PackageSize { get; set; }
        public string PackageHash { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public DateTimeOffset Created { get; set; }
    }
}
