using NuGet.Client;
using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NuGet.Client.V2.VisualStudio
{
    
    public class V2VisualStudioUIMetadataResource : V2Resource, IVisualStudioUIMetadata
    {
        public V2VisualStudioUIMetadataResource(IPackageRepository repo, string host) : base(repo, host) { }      

        Task<VisualStudioUIPackageMetadata> IVisualStudioUIMetadata.GetPackageMetadataForVisualStudioUI(string packageId, NuGetVersion version)
        {
            throw new NotImplementedException();
        }
    }
}
