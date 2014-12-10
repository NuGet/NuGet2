using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.BaseTypes.Respository
{
    public interface IResolutionResource
    {
        PackageIdentity ResolvePackage(string id, string version);
    }
}
