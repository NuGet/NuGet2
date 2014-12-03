using NuGet;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    public class V2DownloadResource : V2Resource,IDownload
    {       
        public V2DownloadResource(V2Resource v2Resource)
            : base(v2Resource) {}
      
        public PackageDownloadMetadata GetNupkgUrlForDownload(PackageIdentity identity)
        {
            throw new NotImplementedException();
        }
    }
}
