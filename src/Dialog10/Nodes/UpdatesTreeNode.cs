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

            // The allow prerelease flag passed to this method indicates if we are allowed to show prerelease packages as part of the updates 
            // and does not reflect the filtering of packages we are looking for updates to.
            var packages = _localRepository.GetPackages();
            if (!String.IsNullOrEmpty(searchTerm))
            {
                packages = packages.Find(searchTerm);
            }

            // Fix Bug #3034: When showing updates in solution-level dialog, it can happen that the local repository includes
            // both jQuery 1.7 and jQuery 1.9 (from two different projects). To shows update for the jQuery 1.7, we need to make
            // sure to remove jQuery 1.9 from the list.
            // To do so, we sort all packages by increasing version, and call Distinct() which effectively remove higher versions of each package id
            List<IPackage> packagesList = packages.ToList();
            if (packagesList.Count > 0)
            {
                packagesList.Sort(PackageComparer.Version);
                packagesList = packagesList.Distinct(PackageEqualityComparer.Id).ToList();
            }

            // Tf the local repository contains constraints for each package, we send the version constraints to the GetUpdates() service.
            IPackageConstraintProvider constraintProvider = _localRepository as IPackageConstraintProvider;
            if (constraintProvider != null)
            {
                IEnumerable<IVersionSpec> constraintList = packagesList.Select(p => constraintProvider.GetConstraint(p.Id));
                return Repository.GetUpdates(packagesList, allowPrereleaseVersions, includeAllVersions: false, targetFrameworks: solutionFrameworks, versionConstraints: constraintList)
                                 .AsQueryable();
            }

            return Repository.GetUpdates(packagesList, allowPrereleaseVersions, includeAllVersions: false, targetFrameworks: solutionFrameworks)
                             .AsQueryable();
        }

        protected override IQueryable<IPackage> CollapsePackageVersions(IQueryable<IPackage> packages)
        {
            // GetUpdates collapses package versions to start with. Additionally, our method of using the IsLatest might not work here because
            // we might have a package that is not the latest version but is the only compatible package that satisfies the result set.
            return packages;
        }
    }
}