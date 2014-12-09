using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    public abstract class MetadataResource
    {
        public abstract NuGetVersion GetLatestVersionForPowershellAndCommandLine(string packageId);
        public abstract VisualStudioUIPackageMetaData GetPackageMetadataForVisualStudioUI(string packageId);
    }
}
