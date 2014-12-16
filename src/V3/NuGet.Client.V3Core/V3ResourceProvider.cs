using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V3
{
    public abstract class V3ResourceProvider : IResourceProvider
    {
        public override bool TryCreateResource(PackageSource source, ref IDictionary<string, object> cache, out Resource resource)
        {
            try
            {
                string host = "TestHost";
                if (V3Utilities.IsV3(source))
                {
                    object repo = null;
                    if (!cache.TryGetValue(source.Url, out repo))
                    {
                        repo = V3Utilities.GetV3Client(source, host);
                        cache.Add(source.Url, repo);
                    }
                    resource = new V3Resource((NuGetV3Client)repo);
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
