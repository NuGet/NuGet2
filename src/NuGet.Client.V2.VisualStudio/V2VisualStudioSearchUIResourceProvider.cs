using System.ComponentModel.Composition;
using NuGet.Client.VisualStudio.Models;

namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("VsV2SearchResourceProvider", typeof(IVisualStudioUISearch))]
    public class V2VisualStudioUISearchResourceProvider : V2ResourceProvider
    {
        public override async System.Threading.Tasks.Task<Resource> Create(PackageSource source)
        {
            var resource = await base.Create(source);
            if (resource != null)
            {
                var vsV2SearchResource = new V2VisualStudioUISearchResource((V2Resource)resource);
                return vsV2SearchResource;
            }
            else
            {
                return null;
            }
        }
    }
}