using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    public abstract class ResolutionResource
    {
        public abstract PackageIdentity ResolvePackage(string id, string version);
        public abstract PackageIdentity ResolveDependency( ??)

)
    }
}
