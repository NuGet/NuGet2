using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {

    internal class OnlinePackagesSearchNode : OnlinePackagesTreeBase {

        private string _searchText;

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
            query = query.Where(p => (p.Description != null && p.Description.ToUpper().Contains(_searchText.ToUpper())) 
                || (p.Id != null && p.Id.ToUpper().Contains(_searchText.ToUpper())));
            return query;
        }

        protected override void FillNodes(IList<IVsExtensionsTreeNode> nodes) {
            // Do nothing since this node will not have any child nodes
        }
    }
}
