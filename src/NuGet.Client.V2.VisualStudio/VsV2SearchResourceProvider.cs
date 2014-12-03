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
        public override bool TryCreateResource(PackageSource source, out Resource resource)
        {
            VsV2SearchResource vsV2SearchResource;
            if (base.TryCreateResource(source,out resource))
            {
                vsV2SearchResource = new VsV2SearchResource((V2Resource)resource);
                resource = vsV2SearchResource;
                return true;
            }
            else
            {
                return false;
            }
        }       
    }
}
