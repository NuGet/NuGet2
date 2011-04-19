using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PackageExplorerViewModel;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for PackageViewer.xaml
    /// </summary>
    public partial class PackageViewer : UserControl {
        public PackageViewer(IUIServices messageBoxServices, IPackageViewModelFactory packageViewModelFactory) {
            InitializeComponent();

            PackageMetadataEditor.UIServices = messageBoxServices;
            PackageMetadataEditor.PackageViewModelFactory = packageViewModelFactory;
        }

        private void FileContentContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var settings = Properties.Settings.Default;

            if ((bool)e.NewValue) {
                ContentGrid.RowDefinitions[2].Height = new GridLength(settings.ContentViewerPanelHeight, GridUnitType.Pixel);

                if (FileContentContainer.Content == null) {
                    UserControl fileContent = CreateFileContentViewer();
                    FileContentContainer.Content = fileContent;
                }
            }
            else {
                settings.ContentViewerPanelHeight = ContentGrid.RowDefinitions[2].Height.Value;
                ContentGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        // delay load the Syntax HighlightTextBox, avoid loading SyntaxHighlighting.dll upfront
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UserControl CreateFileContentViewer() {
            var content = new ContentViewerPane();
            content.SetBinding(UserControl.DataContextProperty, new Binding("CurrentFileInfo"));
            return content;
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var model = DataContext as PackageViewModel;
            if (model != null) {
                model.SelectedItem = PackagesTreeView.SelectedItem;
            }
        }

        private void OnTreeViewItemDoubleClick(object sender, RoutedEventArgs args) {
            var item = (TreeViewItem)sender;
            PackageFile file = item.DataContext as PackageFile;
            if (file != null) {
                var command = ((PackageViewModel)DataContext).ViewContentCommand;
                command.Execute(file);

                args.Handled = true;
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            TreeView tv = (TreeView)sender;
            IInputElement element = tv.InputHitTest(e.GetPosition(tv));
            while (!((element is TreeView) || element == null)) {
                if (element is TreeViewItem)
                    break;

                if (element is FrameworkElement) {
                    FrameworkElement fe = (FrameworkElement)element;
                    element = (IInputElement)(fe.Parent ?? fe.TemplatedParent);
                }
                else if (element is FrameworkContentElement) {
                    FrameworkContentElement fe = (FrameworkContentElement)element;
                    element = (IInputElement)fe.Parent;
                }
                else
                    break;
            }
            if (element is TreeViewItem) {
                element.Focus();
                e.Handled = true;
            }
        }

        private void OnTreeViewItemDragOver(object sender, DragEventArgs e) {
            PackageFolder folder;

            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                folder = item.DataContext as PackageFolder;
            }
            else {
                folder = (DataContext as PackageViewModel).RootFolder;
            }
            if (folder != null) {
                var data = e.Data;
                if (data.GetDataPresent(DataFormats.FileDrop)) {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnTreeViewItemDrop(object sender, DragEventArgs e) {
            PackageFolder folder = null;

            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                folder = item.DataContext as PackageFolder;
            }

            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    var viewModel = DataContext as PackageViewModel;
                    viewModel.AddDraggedAndDroppedFiles(folder, filenames);

                    e.Handled = true;
                }
            }
        }
    }
}