using NuGet.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Resource provider for V2 download.
    /// </summary>
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("V2DownloadResourceProvider", typeof(IDownload))]
    public class V2DownloadResourceProvider : V2ResourceProvider
    {
        public override async Task<Resource> Create(PackageSource source)
        {
            var resource = await base.Create(source);
            if (resource != null)
            {
                var v2DownloadResource = new V2DownloadResource((V2Resource)resource);
                return v2DownloadResource;
            }
            else
            {
                return null;
            }
        }
    }
}
