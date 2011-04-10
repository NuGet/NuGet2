using System;
using System.Globalization;
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

        private readonly IMessageBox _messageBoxServices;

        public PackageViewer(IMessageBox messageBoxServices, IPackageViewModelFactory packageViewModelFactory) {

            InitializeComponent();

            _messageBoxServices = messageBoxServices;
            PackageMetadataEditor.MessageBox = messageBoxServices;
            PackageMetadataEditor.PackageViewModelFactory = packageViewModelFactory;
        }

        private void AddRootFolderExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext == null) {
                return;
            }

            var rootFolder = (DataContext as PackageViewModel).RootFolder;
            string subFolder = (string)e.Parameter;
            rootFolder.AddFolder(subFolder);
            e.Handled = true;
        }

        private void AddRootFolderCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            if (DataContext == null) {
                e.CanExecute = false;
                e.Handled = true;
                return;
            }

            var rootFolder = (DataContext as PackageViewModel).RootFolder;
            string subFolder = (string)e.Parameter;
            e.CanExecute = !rootFolder.ContainsFolder(subFolder);
            e.Handled = true;
        }

        private void PackagesTreeView_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var selectedPart = PackagesTreeView.SelectedItem as PackagePart;
                if (selectedPart != null) {
                    selectedPart.Delete();
                }
                e.Handled = true;
            }
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

        private void OnAddNewFolder2Click(object sender, RoutedEventArgs e) {
            var folder = (DataContext as PackageViewModel).RootFolder;
            if (folder != null) {
                var dialog = new RenameWindow {
                    NewName = "NewFolder",
                    Owner = Window.GetWindow(this)
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    string newName = dialog.NewName;
                    folder.AddFolder(newName);
                }
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var model = DataContext as PackageViewModel;
            if (model != null) {
                model.SelectedItem = PackagesTreeView.SelectedItem;
            }
        }

        private void OnAddNewFolderClick(object sender, RoutedEventArgs e) {
            MenuItem menuItem = (MenuItem)sender;
            PackageFolder folder = menuItem.DataContext as PackageFolder;

            if (folder == null) {
                folder = PackagesTreeView.SelectedItem as PackageFolder;
            }

            if (folder == null) {
                folder = (DataContext as PackageViewModel).RootFolder;
            }

            if (folder != null) {
                var dialog = new RenameWindow {
                    NewName = "NewFolder",
                    Owner = Window.GetWindow(this)
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    string newName = dialog.NewName;
                    folder.AddFolder(newName);
                }
            }
        }

        private void OnRenameItemClick(object sender, RoutedEventArgs e) {
            MenuItem menuItem = (MenuItem)sender;
            PackagePart part = menuItem.DataContext as PackagePart;
            if (part == null) {
                part = PackagesTreeView.SelectedItem as PackagePart;
            }

            if (part != null) {
                var dialog = new RenameWindow {
                    NewName = part.Name,
                    Owner = Window.GetWindow(this)
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    part.Rename(dialog.NewName);
                }
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
                    foreach (string file in filenames) {
                        AddFileToFolder(folder, file);
                    }

                    e.Handled = true;
                }
            }
        }

        private void AddFileToFolder(PackageFolder folder, string file) {
            if (folder == null) {
                string guessFolderName = GuessFolderNameFromFile(file);
                bool confirmed = _messageBoxServices.Confirm(
                    String.Format(CultureInfo.CurrentCulture, "Do you want to place the file '{0}' into '{1}' folder?", file, guessFolderName));

                var rootFolder = (DataContext as PackageViewModel).RootFolder;

                if (confirmed) {
                    if (rootFolder.ContainsFolder(guessFolderName)) {
                        folder = (PackageFolder)rootFolder[guessFolderName];
                    }
                    else {
                        folder = rootFolder.AddFolder(guessFolderName);
                    }
                }
                else {
                    folder = rootFolder;
                }
            }

            folder.AddFile(file);
        }

        private static string GuessFolderNameFromFile(string file) {
            string extension = System.IO.Path.GetExtension(file).ToUpperInvariant();
            if (extension == ".DLL" || extension == ".PDB") {
                return "lib";
            }
            else if (extension == ".PS1" || extension == ".PSM1" || extension == ".PSD1") {
                return "tools";
            }
            else {
                return "content";
            }
        }
    }
}
