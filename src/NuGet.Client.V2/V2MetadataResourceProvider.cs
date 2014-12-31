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
        public override async Task<Resource> Create(PackageSource source)
        {
            var resource = await base.Create(source);
            if (resource != null)
            {
                var v2MetadataResource = new V2MetadataResource((V2Resource)resource);
                return v2MetadataResource;
            }
            else
            {
                return null;
            }
        }
    }
}
