using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    public class V2MetadataResource : V2Resource, IMetadata
    {
        public V2MetadataResource(V2Resource resource)
            : base(resource) {}
        public Task<Versioning.NuGetVersion> GetLatestVersion(string packageId)
        {
            //*TODOs : No special processing for UNC or local share. Let the IPackageRepo handle it as it does today as of now.
             return Task.Factory.StartNew(() =>
            {
                SemanticVersion latestVersion = V2Client.FindPackagesById(packageId).OrderByDescending(p => p.Version).FirstOrDefault().Version;
                return new NuGetVersion(latestVersion.Version, latestVersion.SpecialVersion);
            });
        }

        public Task<bool> IsSatellitePackage(string packageId)
        {
            throw new NotImplementedException();
        }
    }
}
