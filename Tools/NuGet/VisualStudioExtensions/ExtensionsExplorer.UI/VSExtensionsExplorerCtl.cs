using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.ExtensionsExplorer.UI
{
    public class VSExtensionsExplorerCtl : UserControl
    {
        public ViewStyle ActiveView { get; set; }
        public bool ListViewIsVirtualizing { get; set; }
        public bool IsFxComboVisible { get; set; }
        public bool IsMediumIconsViewButtonVisible { get; set; }
        public bool IsSmallIconsViewButtonVisible { get; set; }
        public bool IsLargeIconsViewButtonVisible { get; set; }

        public Guid SearchCategory { get; set; }
        public IVsExtensionsProvider SelectedProvider { get; set; }
        public IList<IVsExtensionsProvider> Providers
        {
            get { return null; }
        }

        public IVsExtension SelectedExtension { get; set; }
        public IVsExtensionsTreeNode SelectedExtensionTreeNode { get; set; }

        public void SetFocusOnSearchBox()
        {
        }

        public object SearchControlParent { get { return null; } }

        public string NoItemsMessage { get; set; }

        public event RoutedPropertyChangedEventHandler<object> CategorySelectionChanged;
        public event RoutedPropertyChangedEventHandler<object> ProviderSelectionChanged;
    }
}

