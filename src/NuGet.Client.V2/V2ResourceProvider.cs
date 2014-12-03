using System;
using System.Diagnostics.CodeAnalysis;

namespace NuGet.Client.V2
{
    /// <summary>
    /// Partial implementation for IResourceProvider to do the common V2 specific stuff.
    /// </summary>
    public abstract class V2ResourceProvider : ResourceProvider
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override bool TryCreateResource(PackageSource source, out Resource resource)
        {
            try
            {
                object repo = null;
                string host = "TestHost";
                if (!packageSourceCache.TryGetValue(source.Url, out repo)) //Check if the source is already present in the cache.
                {
                    if (V2Utilities.IsV2(source)) //if it's not in cache, then check if it is V2.
                    {
                        repo = V2Utilities.GetV2SourceRepository(source, host); //Get a IPackageRepo object and add it to the cache.
                        packageSourceCache.Add(source.Url, repo);
                    }
                    else
                    {
                        resource = null; //if it's not V2, then return.
                        return false;
                    }
                }
                resource = new V2Resource((IPackageRepository)repo, host); //Create a resource and return it.
                return true;
            }
            catch (Exception)
            {
                resource = null;
                return false; //*TODOs:Do tracing and throw apppropriate exception here.
            }
        }
    }
}