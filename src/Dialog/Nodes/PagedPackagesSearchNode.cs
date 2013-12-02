using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    internal class PagedPackagesSearchNode : PackagesSearchNode, IVsPageDataSource
    {
        public PagedPackagesSearchNode(PackagesProviderBase provider, IVsExtensionsTreeNode parent, PackagesTreeNodeBase baseNode, string searchText) :
            base(provider, parent, baseNode, searchText)
        {
        }
    }
}
