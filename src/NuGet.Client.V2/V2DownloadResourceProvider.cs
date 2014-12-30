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
        public async override Task<Resource> Create(PackageSource source)
        {
            V2DownloadResource v2DownloadResource;
            Resource resource = await base.Create(source);
            if (resource != null)
            {
                v2DownloadResource = new V2DownloadResource((V2Resource)resource);
                resource = v2DownloadResource;
            }
            return resource;
            
        }
    }
}
