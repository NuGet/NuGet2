using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.ClientV3
{
    public abstract class PackageManagerSession
    {
        public abstract PackageSource ActiveSource { get; }
        public abstract string Name { get; }

        public abstract IEnumerable<PackageSource> GetEnabledSources();
        public abstract void ChangeActiveSource(string newSourceName);
        public abstract IPackageSearcher CreateSearcher();
        
        public abstract SemanticVersion GetInstalledVersion(string id);
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();
        public abstract IEnumerable<IPackageName> GetInstalledPackages();
        public abstract bool IsInstalled(string id, SemanticVersion version);
        public abstract Task<IEnumerable<PackageAction>> ResolveActionsAsync(PackageAction action, string packageId, SemanticVersion packageVersion);
        public abstract Task ExecuteActions(IEnumerable<PackageAction> actions);
    }
}
