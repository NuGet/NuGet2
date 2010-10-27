using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    internal class PackagesSearchNode : PackagesTreeNodeBase {

        private readonly string _searchText;
        private readonly PackagesTreeNodeBase _baseNode;

        public PackagesSearchNode(PackagesProviderBase provider, IVsExtensionsTreeNode parent, PackagesTreeNodeBase baseNode, string searchText) :
            base(parent, provider) {

            if (baseNode == null) {
                throw new ArgumentNullException("baseNode");
            }

            _searchText = searchText;
            _baseNode = baseNode;

            // Mark this node as a SearchResults node to assist navigation in ExtensionsExplorer
            IsSearchResultsNode = true;
        }

        public override string Name {
            get {
                return Resources.Dialog_RootNodeSearch;
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            return _baseNode.GetPackages().Find(_searchText);
        }
    }
}
