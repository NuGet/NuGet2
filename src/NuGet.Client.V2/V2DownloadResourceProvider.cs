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
        public override bool TryCreateResource(PackageSource source, out Resource resource)
        {
            V2DownloadResource v2DownloadResource;
           if(base.TryCreateResource(source, out resource))
           {
               v2DownloadResource = new V2DownloadResource((V2Resource)resource);
               resource = v2DownloadResource;
               return true;
           }
           else
           {
               return false;
           }
        }
    }
}
