using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("V2MetadataResourceProvider", typeof(IMetadata))]
    public class V2MetadataResourceProvider : V2ResourceProvider
    {
        public async override Task<Resource> Create(PackageSource source)
        {
            V2MetadataResource v2MetadataResource;
            Resource resource = await base.Create(source);
            if (resource != null)
            {
                v2MetadataResource = new V2MetadataResource((V2Resource)resource);
                resource = v2MetadataResource;
            }
            return resource;
        }
    }
}
