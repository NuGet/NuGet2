using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("VsV2SearchResourceProvider", typeof(IVsSearch))]
    public class VsV2SearchResourceProvider : V2ResourceProvider
    {
        public async override Task<Resource> Create(PackageSource source)
        {
            VsV2SearchResource vsV2SearchResource;
            Resource resource = await base.Create(source);
            if (resource != null)
            {
                vsV2SearchResource = new VsV2SearchResource((V2Resource)resource);
                resource = vsV2SearchResource;
            }
            return resource;
        }       
    }
}
