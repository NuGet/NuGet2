using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.PackageManagerUI.Providers
{
    class PackageTreeNode : IVsExtensionsTreeNode, IVsPageDataSource, IVsProgressPaneConsumer
    {
        ObservableCollection<IVsExtension> _extensions = new ObservableCollection<IVsExtension>();
        ObservableCollection<IVsExtensionsTreeNode> _nodes = new ObservableCollection<IVsExtensionsTreeNode>();

        public ObservableCollection<IVsExtension> VSExtensions
        {
            get { return _extensions; }
        }

        public IList<IVsExtension> Extensions
        {
            get { return _extensions; }
        }

        public bool IsExpanded
        {
            get;
            set;
        }

        public bool IsSearchResultsNode
        {
            get;
            private set;
        }

        public bool IsSelected
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public IList<IVsExtensionsTreeNode> Nodes
        {
            get { return _nodes; }
        }

        public IVsExtensionsTreeNode Parent
        {
            get;
            set;
        }

        int IVsPageDataSource.CurrentPage
        {
            get { return 1; }
        }

        void IVsPageDataSource.LoadPage(int pageNumber)
        {
            
        }

        event EventHandler<EventArgs> IVsPageDataSource.PageDataChanged
        {
            add {  }
            remove {  }
        }

        int IVsPageDataSource.TotalPages
        {
            get { return 10; }
        }

        void IVsProgressPaneConsumer.SetProgressPane(IVsProgressPane progressPane)
        {
            
        }
    }
}
