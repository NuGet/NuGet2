using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Repository
{
    public interface IVsMetadataResource
    {
       VisualStudioUIPackageMetadata GetPackageMetadataForVisualStudioUI(string packageId, NuGetVersion version);
    }
}
