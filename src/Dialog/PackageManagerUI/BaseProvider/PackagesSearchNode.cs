using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers {

    internal class PackagesSearchNode : PackagesTreeNodeBase {

        private string _searchText;
        private readonly PackagesTreeNodeBase _baseNode;

        public PackagesTreeNodeBase BaseNode {
            get {
                return _baseNode;
            }
        }

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

        public void SetSearchText(string newSearchText) {
            if (newSearchText == null) {
                throw new ArgumentNullException("newSearchText");
            }

            _searchText = newSearchText;

            if (IsSelected) {
                ResetQuery();
                LoadPage(1);
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            return _baseNode.GetPackages().Find(_searchText);
        }
    }
}