using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Resources
{
    public abstract class PowerShellAutoCompleteResource
    {
        public abstract IEnumerable<string> GetPackageIdsStartingWith(string packageIdPrefix);
        public abstract IEnumerable<NuGetVersion> GetPackageVersionsStartingWith(string versionPrefix);
    }
}
