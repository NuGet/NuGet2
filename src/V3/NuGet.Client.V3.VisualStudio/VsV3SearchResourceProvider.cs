using NuGet.Client.VisualStudio.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3.VisualStudio
{
    [Export(typeof(IResourceProvider))]
    [ResourceProviderMetadata("VsV2SearchResourceProvider", typeof(VsSearchResource))]
    public class VsV3SearchResourceProvider : IResourceProvider
    {
        public bool TryCreateResource(PackageSource source, ref IDictionary<string, object> cache, out Resource resource)
        {
            throw new NotImplementedException();
        }

        public Resource Create(PackageSource source, ref IDictionary<string, object> cache)
        {
            throw new NotImplementedException();
        }
    }
}
