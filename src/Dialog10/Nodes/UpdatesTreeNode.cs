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

        public override IQueryable<IPackage> GetPackages()
        {
            // We need to call ToList() here so that we don't evaluate the enumerable twice
            // when trying to count it.
            IList<IPackage> updateCandidates = Repository.GetUpdates(_localRepository.GetPackages(), includePrerelease: false).ToList();

            IList<FrameworkName> solutionFrameworks = Provider.SupportedFrameworks.Select(s => new FrameworkName(s)).ToList();

            // among the candidates, choose those that are compatible with at least one project in the solution
            var updates = from package in updateCandidates
                          let packageFrameworks = package.GetSupportedFrameworks()
                          where solutionFrameworks.Count == 0 || solutionFrameworks.Any(s => VersionUtility.IsCompatible(s, packageFrameworks))
                          select package;

            return updates.AsQueryable();
        }
    }
}
