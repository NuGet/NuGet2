using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.Providers;
using NuGet.Test;

namespace NuGet.Dialog.Test
{
    /// <summary>
    /// Concrete class to assist in testing the abstract PackagesTreeNodeBase
    /// </summary>
    internal class MockTreeNode : PackagesTreeNodeBase, IVsPageDataSource
    {
        private int _numberOfPackages;
        private IEnumerable<IPackage> _packages;
        private readonly bool _supportsPrereleasePackages;

        public override string Name
        {
            get
            {
                return "Mock Tree Node";
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return _supportsPrereleasePackages; }
        }

        public override IQueryable<IPackage> GetPackages(string searchTerm, bool allowPrereleaseVersions)
        {
            if (_packages == null)
            {
                var packages = new List<IPackage>();
                for (int i = 0; i < _numberOfPackages; i++)
                {
                    packages.Add(PackageUtility.CreatePackage("A" + i, "1.0", downloadCount: i));
                }
                _packages = packages;
            }

            var results = _packages.FilterByPrerelease(allowPrereleaseVersions)
                                   .AsQueryable();
            if (!String.IsNullOrEmpty(searchTerm))
            {
                results = results.Find(searchTerm);
            }

            return results;
        }

        public MockTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, int numberOfPackages, bool collapseVersions, bool supportsPrereleasePackages = true)
            : base(parent, provider, collapseVersions)
        {
            _numberOfPackages = numberOfPackages;
            _supportsPrereleasePackages = supportsPrereleasePackages;
        }

        public MockTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, IEnumerable<IPackage> packages, bool collapseVersions, bool supportsPrereleasePackages = true)
            : base(parent, provider, collapseVersions)
        {
            _supportsPrereleasePackages = supportsPrereleasePackages;
            _packages = packages;
        }
    }
}