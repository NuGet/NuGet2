using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// This tree node lists all packages from a fixed repository.
    /// </summary>
    internal class SimpleTreeNode : PackagesTreeNodeBase
    {
        private readonly IPackageRepository _repository;
        private readonly string _category;

        public IPackageRepository Repository
        {
            get
            {
                return _repository;
            }
        }

        public SimpleTreeNode(PackagesProviderBase provider, string category, IVsExtensionsTreeNode parent, IPackageRepository repository, bool collapseVersion = true) :
            base(parent, provider, collapseVersion)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _category = category;
            _repository = repository;
        }

        public override string Name
        {
            get
            {
                return _category;
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get 
            {
                return Repository.SupportsPrereleasePackages;
            }
        }

        public override IQueryable<IPackage> GetPackages(string searchTerm, bool allowPrereleaseVersions)
        {
            return Repository.Search(searchTerm: searchTerm, targetFrameworks: Provider.SupportedFrameworks, allowPrereleaseVersions: allowPrereleaseVersions);
        }
    }
}