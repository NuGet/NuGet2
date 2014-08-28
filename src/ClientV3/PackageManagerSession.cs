using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Tools
{
    public abstract class PackageManagerSession
    {
        public abstract string Name { get; }
        public abstract PackageSource ActiveSource { get; }
        
        public abstract IEnumerable<PackageSource> GetAvailableSources();
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();
        public abstract IPackageSearcher GetSearcher();
        public abstract IInstalledPackageList GetInstalledPackages();
        public abstract void ChangeActiveSource(string newSourceName);
    }
}
