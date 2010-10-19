using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {

    internal class OnlinePackagesSearchNode : OnlinePackagesTreeBase {

        private readonly string _searchText;

        public OnlinePackagesSearchNode(OnlinePackagesProvider provider, IVsExtensionsTreeNode parent, string searchText) :
            base(parent, provider) {

            _searchText = searchText;

            // Mark this node as a SearchResults node to assist navigation in ExtensionsExplorer
            IsSearchResultsNode = true;
        }

        public override string Name {
            get {
                return Resources.Dialog_RootNodeSearch;
            }
        }

        protected override IQueryable<IPackage> PreviewQuery(IQueryable<IPackage> query) {
            return query.Find(_searchText);
        }

        protected override void FillNodes(IList<IVsExtensionsTreeNode> nodes) {
            // Do nothing since this node will not have any child nodes
        }
    }
}
