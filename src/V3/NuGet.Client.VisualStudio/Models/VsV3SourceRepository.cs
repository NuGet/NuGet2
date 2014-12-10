using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.Repository
{
    public class VsV3SourceRepository : V3SourceRepository2
    {
        public VsV3SourceRepository(PackageSource source, string host):base(source,host)
        {
            AddResource<VsV3SearchResource>(() => new VsV3SearchResource(Source.Url, host));
            AddResource<VsV3MetadataResource>(() => new VsV3MetadataResource(Source.Url, host));
        }
    }
}
