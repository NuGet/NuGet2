using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.Providers;
using NuGet.Test;

namespace NuGet.Dialog.Test {
    /// <summary>
    /// Concrete class to assist in testing the abstract PackagesTreeNodeBase
    /// </summary>
    internal class MockTreeNode : PackagesTreeNodeBase {

        private int _numberOfPackages;
        private IPackage[] _packages;

        public override string Name {
            get {
                return "Mock Tree Node";
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            if (_packages == null) {
                _packages = new IPackage[_numberOfPackages];
                for (int i = 0; i < _numberOfPackages; i++) {
                    _packages[i] = PackageUtility.CreatePackage("A" + i, "1.0");
                }
            }

            return _packages.AsQueryable();
        }

        public MockTreeNode(IVsExtensionsTreeNode parent, PackagesProviderBase provider, int numberOfPackages)
            : base(parent, provider) {

            _numberOfPackages = numberOfPackages;
        }
    }
}
