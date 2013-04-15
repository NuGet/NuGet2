using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// Base implementation of IVsExtensionsTreeNode
    /// </summary>
    public class RootPackagesTreeNode : IVsExtensionsTreeNode
    {
        private readonly IList<IVsExtensionsTreeNode> nodes = new ObservableCollection<IVsExtensionsTreeNode>();

#if VS10
        private readonly IList<IVsExtension> extensions = new ObservableCollection<IVsExtension>();

        public IList<IVsExtension> Extensions
        {
            get { return extensions; }
        }
#else
        private readonly IList extensions = new ObservableCollection<IVsExtension>();

        public IList Extensions
        {
            get { return extensions; }
        }
#endif

        public bool IsSearchResultsNode
        {
            get { return false; }
        }

        public bool IsExpanded
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get;
            set;
        }

        public string Name
        {
            get { return string.Empty; }
        }

        public IList<IVsExtensionsTreeNode> Nodes
        {
            get { return nodes; }
        }

        public IVsExtensionsTreeNode Parent
        {
            get { return null; }
        }
    }
}
