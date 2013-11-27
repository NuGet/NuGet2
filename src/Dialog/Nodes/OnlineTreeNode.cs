using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// Represents a tree node displaying list of online packages.
    /// </summary>
    internal class OnlineTreeNode : SimpleTreeNode, IVsPageDataSource
    {
        public OnlineTreeNode(
            PackagesProviderBase provider,
            string category,
            IVsExtensionsTreeNode parent,
            IPackageRepository repository) :
            base(provider, category, parent, repository)
        {
        }
    }
}
