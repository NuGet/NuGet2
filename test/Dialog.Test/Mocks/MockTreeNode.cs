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
    internal class MockTreeNode : PackagesTreeNodeBase
    {

        private int _numberOfPackages;
        private IEnumerable<IPackage> _packages;

        public override string Name
        {
            get
            {
                return "Mock Tree Node";
            }
        }

        public override IQueryable<IPackage> GetPackages(bool allowPrereleaseVersions)
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

            return _packages.AsQueryable().FilterByPrerelease(allowPrereleaseVersions);
        }

        public MockTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, int numberOfPackages, bool collapseVersions)
            : base(parent, provider, collapseVersions)
        {
            _numberOfPackages = numberOfPackages;
        }

        public MockTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, IEnumerable<IPackage> packages, bool collapseVersions)
            : base(parent, provider, collapseVersions)
        {

            _packages = packages;
        }
    }
}