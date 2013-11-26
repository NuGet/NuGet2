using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    internal class PackagesSearchNode : PackagesTreeNodeBase
    {
        private string _searchText;
        private readonly PackagesTreeNodeBase _baseNode;

        public PackagesSearchNode(PackagesProviderBase provider, IVsExtensionsTreeNode parent, PackagesTreeNodeBase baseNode, string searchText) :
            base(parent, provider, baseNode.CollapseVersions)
        {
            if (baseNode == null)
            {
                throw new ArgumentNullException("baseNode");
            }

            _searchText = searchText;
            _baseNode = baseNode;

            // Mark this node as a SearchResults node to assist navigation in ExtensionsExplorer
            IsSearchResultsNode = true;
        }

        public PackagesTreeNodeBase BaseNode
        {
            get
            {
                return _baseNode;
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return BaseNode.SupportsPrereleasePackages; }
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_RootNodeSearch;
            }
        }

        public void SetSearchText(string newSearchText)
        {
            if (newSearchText == null)
            {
                throw new ArgumentNullException("newSearchText");
            }

            _searchText = newSearchText;

            if (IsSelected)
            {
                ResetQuery();
                LoadPage(1);
            }
        }

        public override IQueryable<IPackage> GetPackages(string searchTerm, bool allowPrereleaseVersions)
        {
            return _baseNode.GetPackages(_searchText, allowPrereleaseVersions);
        }

        protected override IQueryable<IPackage> ApplyOrdering(IQueryable<IPackage> query)
        {
            if (Provider.CurrentSort == OnlineSearchProvider.RelevanceSortDescriptor)
            {
                // If we are sorting by relevance, then do nothing. 
                return query;
            }
            return base.ApplyOrdering(query);
        }
    }    
}