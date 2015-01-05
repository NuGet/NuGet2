using System.ComponentModel.Composition;
using NuGet.Client.VisualStudio.Models;

namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(ResourceProvider))]
    [ResourceProviderMetadata("V2PowerShellAutocompleteResourceProvider", typeof(IPowerShellAutoComplete))]
    public class V2PowerShellAutoCompleteResourceProvider : V2ResourceProvider
    {
        public override async System.Threading.Tasks.Task<Resource> Create(PackageSource source)
        {
            var resource = await base.Create(source);
            if (resource != null)
            {
                var vsV2SearchResource = new V2PowerShellAutoCompleteResource((V2Resource)resource);
                return vsV2SearchResource;
            }
            else
            {
                return null;
            }
        }
    }
}
