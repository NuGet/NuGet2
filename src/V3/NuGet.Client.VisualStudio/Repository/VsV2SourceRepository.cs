using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.Interop;
using NuGet.Client.Resources;

namespace NuGet.Client.VisualStudio.Repository
{
    public class VsV2SourceRepository : V2SourceRepository2
    {
        public VsV2SourceRepository(PackageSource source,IPackageRepository repository,string host): base(source,repository,host)
        {           
            AddResource<VsV2SearchResource>(() => new VsV2SearchResource(repository,host));          
        }
    }
}
