using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Partial implementation for IResourceProvider to do the common V2 specific stuff.
    /// </summary>
    public abstract class V2ResourceProvider : ResourceProvider
    {
        public override async Task<Resource> Create(PackageSource source)
        {
            try
            {
                object repo = null;
                string host = "TestHost";

                // Check if the source is already present in the cache.
                if (!packageSourceCache.TryGetValue(source.Url, out repo))
                {
                    // if it's not in cache, then check if it is V2.
                    if (await V2Utilities.IsV2(source))
                    {
                        // Get a IPackageRepo object and add it to the cache.
                        repo = V2Utilities.GetV2SourceRepository(source, host);
                        packageSourceCache.Add(source.Url, repo);
                    }
                    else
                    {
                        // if it's not V2, returns null
                        return null;
                    }
                }

                // Create a resource and return it.
                var resource = new V2Resource((IPackageRepository)repo, host);
                return resource;
            }
            catch (Exception)
            {
                // *TODOs:Do tracing and throw apppropriate exception here.
                return null; 
            }
        }
    }
}