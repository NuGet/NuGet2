using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    internal class OnlinePackagesSearchNode : OnlinePackagesTreeBase {
        #region Private Members
        private string SearchText { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor - requires provider
        /// </summary>
        /// <param name="provider">Instance of IVsTemplateProvider</param>
        public OnlinePackagesSearchNode(OnlinePackagesProvider provider, IPackageRepository repository, IVsExtensionsTreeNode parent, string searchText) :
            base(repository, parent, provider) {
            this.SearchText = searchText;

            // Mark this node as a SearchResults node to assist navigation in ExtensionsExplorer
            this.IsSearchResultsNode = true;
        }

        #endregion

        public override string Name {
            get {
                return "Search Results";
            }
        }

        protected override IQueryable<Package> PreviewQuery(IQueryable<Package> query) {
            query = query.Where(p => (p.Description != null && p.Description.ToUpper().Contains(SearchText.ToUpper())) 
                || (p.Id != null && p.Id.ToUpper().Contains(SearchText.ToUpper())));
            return query;
        }

        protected override void FillNodes(IList<IVsExtensionsTreeNode> nodes) {
            // Do nothing since this node will not have any child nodes
        }
    }
}
