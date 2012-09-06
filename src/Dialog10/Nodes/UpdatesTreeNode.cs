using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// This class represents a tree node under the Updates tab
    /// </summary>
    internal class UpdatesTreeNode : SimpleTreeNode
    {
        private readonly IPackageRepository _localRepository;

        public UpdatesTreeNode(
            PackagesProviderBase provider,
            string category,
            IVsExtensionsTreeNode parent,
            IPackageRepository localRepository,
            IPackageRepository sourceRepository) :
            base(provider, category, parent, sourceRepository)
        {
            _localRepository = localRepository;
        }

        public override IQueryable<IPackage> GetPackages(string searchTerm, bool allowPrereleaseVersions)
        {
            // We need to call ToList() here so that we don't evaluate the enumerable twice
            // when trying to count it.
            IList<FrameworkName> solutionFrameworks = Provider.SupportedFrameworks.Select(s => new FrameworkName(s)).ToList();

            // The allow prerelease flag passed to this method indiciates if we are allowed to show prerelease packages as part of the updates and does not
            // reflect the filtering of packages we are looking for updates to.
            var packages = _localRepository.GetPackages();
            if (!String.IsNullOrEmpty(searchTerm))
            {
                packages = packages.Find(searchTerm);
            }

            return Repository.GetUpdates(packages, allowPrereleaseVersions, includeAllVersions: false, targetFramework: solutionFrameworks)
                             .AsQueryable();
        }

        protected override IQueryable<IPackage> CollapsePackageVersions(IQueryable<IPackage> packages)
        {
            // GetUpdates collapses package versions to start with. Additionally, our method of using the IsLatest might not work here because we might have a package that 
            // is not the latest version but is the only compatible package that satisfies the result set.
            return packages;
        }
    }
}