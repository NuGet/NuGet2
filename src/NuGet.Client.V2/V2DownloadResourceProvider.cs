using NuGet.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    /// <summary>
    /// *TODOs: Pass in the host name all the way from TryGetResource();
    /// </summary>
    [Export(typeof(IResourceProvider))]
    [ResourceProviderMetadata("V2DownloadResourceProvider", typeof(IDownload))]
    public class V2DownloadResourceProvider : IResourceProvider
    {
        public bool TryCreateResource(PackageSource source, ref IDictionary<string, object> cache, out Resource resource)
        {
            try
            {
                string host = "TestHost";
                if (V2Utilities.IsV2(source))
                {
                    object repo = null;
                    if (!cache.TryGetValue(source.Url, out repo))
                    {
                        repo = V2Utilities.GetV2SourceRepository(source, host);
                        cache.Add(source.Url, repo);
                    }
                    resource = new V2DownloadResource((IPackageRepository)repo, host);
                    return true;
                }
                else
                {
                    resource = null;
                    return false;
                }
            }catch(Exception)
            {
                resource = null;
                return false; //*TODOs:Do tracing and throw apppropriate exception here.
            }            
        }

        public Resource Create(PackageSource source, ref IDictionary<string, object> cache)
        {
            Resource resource = null;
            if (TryCreateResource(source, ref cache, out resource))
                return resource;
            else
                return null; //*TODOs: Throw ResourceNotCreated exception ?
        }
       
    }
}
