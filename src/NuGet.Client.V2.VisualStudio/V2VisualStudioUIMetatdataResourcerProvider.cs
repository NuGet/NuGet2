using System.ComponentModel.Composition;
using NuGet.Client.VisualStudio.Models;

namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("V2VisualStudioUIMetatdataResourceProvider", typeof(IVisualStudioUIMetadata))]
    public class V2VisualStudioUIMetadataResourceProvider : V2ResourceProvider
    {
        public override async System.Threading.Tasks.Task<Resource> Create(PackageSource source)
        {
            var resource = await base.Create(source);
            if (resource != null)
            {
                var vsV2MetatdataResource = new V2VisualStudioUIMetadataResource((V2Resource)resource);
                return vsV2MetatdataResource;
            }
            else
            {
                return null;
            }
        }
    }
}
