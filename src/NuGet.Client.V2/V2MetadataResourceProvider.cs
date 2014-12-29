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
        public override bool TryCreateResource(PackageSource source, out Resource resource)
        {
            V2MetadataResource v2MetadataResource;
            if (base.TryCreateResource(source, out resource))
            {
                v2MetadataResource = new V2MetadataResource((V2Resource)resource);
                resource = v2MetadataResource;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
