using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Partial implementation for IResourceProvider to do the common V2 specific stuff.
    /// </summary>
    public abstract class V2ResourceProvider : IResourceProvider
    {
        public override bool TryCreateResource(PackageSource source, ref IDictionary<string, object> cache, out Resource resource)
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
                    resource = new V2Resource((IPackageRepository)repo, host);
                    return true;
                }
                else
                {
                    resource = null;
                    return false;
                }
            }
            catch (Exception)
            {
                resource = null;
                return false; //*TODOs:Do tracing and throw apppropriate exception here.
            }         
        }     
    }
}
