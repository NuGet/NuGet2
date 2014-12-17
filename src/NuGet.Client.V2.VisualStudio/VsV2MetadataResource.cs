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
    
    public class VsV2MetadataResource : V2Resource,IVsMetadata
    {
        public VsV2MetadataResource() : base(null, null) { }

        public VsV2MetadataResource(IPackageRepository repo, string host) : base(repo, host) { }
      
        public override string Description
        {
            get { throw new NotImplementedException(); }
        }

        public VisualStudioUIPackageMetadata GetPackageMetadataForVisualStudioUI(string packageId, NuGetVersion version)
        {
            throw new NotImplementedException();
        }
    }
}
