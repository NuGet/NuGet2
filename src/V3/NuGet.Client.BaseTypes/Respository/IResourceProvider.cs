using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client
{

    public abstract class IResourceProvider
    {
        public abstract bool TryCreateResource(PackageSource source,ref IDictionary<string,object> cache, out Resource resource);
        public virtual Resource Create(PackageSource source, ref IDictionary<string, object> cache)
        {
            Resource resource = null;
            if (TryCreateResource(source, ref cache, out resource))
                return resource;
            else
                return null; //*TODOs: Throw ResourceNotCreated exception ?
        }
    }
}
