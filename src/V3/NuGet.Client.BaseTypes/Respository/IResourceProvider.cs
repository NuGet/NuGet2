using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client
{

    public interface IResourceProvider
    {
        bool TryCreateResource(PackageSource source,ref IDictionary<string,object> cache, out Resource resource);
        Resource Create(PackageSource source, ref IDictionary<string, object> cache);
    }
}
