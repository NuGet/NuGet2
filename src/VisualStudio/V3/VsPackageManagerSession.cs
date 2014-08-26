using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio.ClientV3;

namespace NuGet.VisualStudio
{
    public abstract class VsPackageManagerSession : PackageManagerSession
    {
        public override PackageSource ActiveSource { get { return RepositoryManager.ActiveSource; } }
        
        protected IVsRepositoryManager RepositoryManager { get; private set; }

        protected VsPackageManagerSession(IVsRepositoryManager repositoryManager)
        {
            RepositoryManager = repositoryManager;
        }

        public override IEnumerable<PackageSource> GetEnabledSources()
        {
            return RepositoryManager.GetEnabledSources();
        }

        public override void ChangeActiveSource(string newSourceName)
        {
            RepositoryManager.ChangeActiveSource(newSourceName);
        }
        public override IPackageSearcher CreateSearcher()
        {
            return RepositoryManager.GetActiveRepository().CreateSearcher();
        }
    }
}
