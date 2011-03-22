using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using NuGet;
using PackageExplorer.Properties;
using PackageExplorerViewModel;
using PackageExplorerViewModel.Types;
using StringResources = PackageExplorer.Resources.Resources;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export]
    public partial class MainWindow : Window {

        private readonly IMruManager _mruManager; 

        [Import]
        public IPackageViewModelFactory PackageViewModelFactory {
            get;
            set;
        }

        [Import]
        public IMessageBox MessageBox {
            get;
            set;
        }

        [Import]
        public ISettingsManager SettingsManager {
            get;
            set;
        }

        [ImportingConstructor]
        public MainWindow(IMruManager mruManager) {
            InitializeComponent();

            PackageMetadataEditor.MessageBox = MessageBox;
            RecentFilesMenuItem.DataContext = _mruManager = mruManager;
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);

            try {
                LoadSettings();
            }
            catch (Exception) { }
        }

        internal void OpenLocalPackage(string packagePath) {
            if (!File.Exists(packagePath)) {
                MessageBox.Show("File not found at " + packagePath, MessageLevel.Error);
                return;
            }
            PackageSourceItem.SetCurrentValue(ContentControl.ContentProperty, "Loading " + packagePath + "...");
            Dispatcher.BeginInvoke(new Action<string>(OpenLocalPackageCore), DispatcherPriority.Loaded, packagePath);
        }

        private void OpenLocalPackageCore(string packagePath) {
            IPackage package = null;
            
            try {
                string extension = Path.GetExtension(packagePath);
                if (extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    package = new ZipPackage(packagePath);
                }
                else if (extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
                {
                    PackageBuilder builder = new PackageBuilder(packagePath);
                    package = builder.Build();
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, MessageLevel.Error);
                return;
            }

            if (package != null) {
                LoadPackage(package, packagePath, PackageType.LocalPackage);
            }
        }

        private void LoadPackage(IPackage package, string packagePath, PackageType packageType) {
            if (package != null) {
                DataContext = PackageViewModelFactory.CreateViewModel(package, packagePath);
                if (!String.IsNullOrEmpty(packagePath))
                {
                    _mruManager.NotifyFileAdded(packagePath, package.ToString(), packageType);
                }
            }
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            DataContext = PackageViewModelFactory.CreateViewModel(new EmptyPackage(), String.Empty);
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = false,
                ValidateNames = true,
                Filter = StringResources.Dialog_OpenFileFilter
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                OpenLocalPackage(dialog.FileName);
            }
        }

        private void OpenPackageFromNuGetFeed(object sender, RoutedEventArgs e) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                MessageBox.Show(
                    PackageExplorer.Resources.Resources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            var dialog = new PackageChooserDialog() { Owner = this };
            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                var selectedPackage = dialog.SelectedPackage;
                if (selectedPackage != null) {
                    var progressWindow = new DownloadProgressWindow(selectedPackage.DownloadUrl, selectedPackage.ToString()) {
                        Owner = this
                    };

                    result = progressWindow.ShowDialog();
                    if (result ?? false) {
                        selectedPackage.SetData(progressWindow.DownloadedFilePath);
                        LoadPackage(selectedPackage, selectedPackage.DownloadUrl.ToString(), PackageType.DataServicePackage);
                    }
                }
            }
        }

        #region Drag & drop

        private void Window_DragOver(object sender, DragEventArgs e) {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    string firstFile = filenames[0];
                    if (FileUtility.IsSupportedFile(firstFile)) {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            var data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)) {
                object value = data.GetData(DataFormats.FileDrop);
                string[] filenames = value as string[];
                if (filenames != null && filenames.Length > 0) {
                    string firstFile = filenames.FirstOrDefault(f => FileUtility.IsSupportedFile(f));
                    if (firstFile != null) {
                        e.Handled = true;

                        bool canceled = AskToSaveCurrentFile();
                        if (!canceled) {
                            OpenLocalPackage(firstFile);
                        }
                    }
                }
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
                    object value = data.GetData(DataFormats.FileDrop);
                    string[] filenames = value as string[];
                    if (filenames != null && filenames.Length > 0) {
                        foreach (string file in filenames) {
                            folder.AddFile(file);
                        }

                        e.Handled = true;
                    }
                }
            }
        }

        #endregion

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            var link = (Hyperlink)sender;
            UriHelper.OpenExternalLink(link.NavigateUri);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e) {
            var dialog = new AboutWindow() { Owner = this };
            dialog.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            bool isCanceled = AskToSaveCurrentFile();
            e.Cancel = isCanceled;

            if (!isCanceled) {
                try {
                    SaveSettings();
                    _mruManager.OnApplicationExit();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Asks user to save the current file before doing something (e.g. exit, open a new file)
        /// </summary>
        /// <returns>true if user cancels the impending action</returns>
        private bool AskToSaveCurrentFile() {
            if (HasUnsavedChanges) {

                // if there is unsaved changes, ask user for confirmation
                var result = MessageBox.ConfirmWithCancel(StringResources.Dialog_SaveQuestion);
                if (result == null) {
                    return true;
                }

                if (result == true) {
                    var saveCommand = SaveMenuItem.Command;
                    const string parameter = "ForceSave";
                    saveCommand.Execute(parameter);
                }
            }

            return false;
        }

        private bool HasUnsavedChanges {
            get {
                var viewModel = (PackageViewModel)DataContext;
                return (viewModel != null && viewModel.HasEdit);
            }
        }

        private void OnFontSizeItem_Click(object sender, RoutedEventArgs e) {
            var item = (MenuItem)sender;
            int size = Convert.ToInt32(item.Tag);
            SetFontSize(size);
        }

        private void SetFontSize(int size) {
            if (size <= 8 || size >= 50) {
                size = 12;
            }
            Properties.Settings.Default.FontSize = size;

            // check the corresponding font size menu item 
            foreach (MenuItem child in FontSizeMenuItem.Items) {
                int value = Convert.ToInt32(child.Tag);
                child.IsChecked = value == size;
            }
        }

        private void LoadSettings() {
            Settings settings = Properties.Settings.Default;
            SetFontSize(settings.FontSize);
            this.LoadWindowPlacementFromSettings(settings.WindowPlacement);
        }

        private void SaveSettings() {
            Settings settings = Properties.Settings.Default;
            settings.WindowPlacement = this.SaveWindowPlacementToSettings();
        }

        private void ViewFileFormatItem_Click(object sender, RoutedEventArgs e) {
            MenuItem item = (MenuItem)sender;
            string url = (string)item.Tag;
            UriHelper.OpenExternalLink(new Uri(url));
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

        private void FileContentContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var settings = Properties.Settings.Default;

            if ((bool)e.NewValue) {
                ContentGrid.RowDefinitions[2].Height = new GridLength(settings.ContentViewerPanelHeight, GridUnitType.Pixel);

                if (FileContentContainer.Content == null) {
                    RichTextBox fileContent = CreateFileContentViewer();
                    fileContent.TextChanged += (o, args) => {
                        ((RichTextBox)o).ScrollToHome();
                    };
                    FileContentContainer.Content = fileContent;
                }

                ((RichTextBox)FileContentContainer.Content).ScrollToHome();
            }
            else {
                settings.ContentViewerPanelHeight = ContentGrid.RowDefinitions[2].Height.Value;
                ContentGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        // delay load the Syntax HighlightTextBox, avoid loading SyntaxHighlighting.dll upfront
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RichTextBox CreateFileContentViewer() {
            var textBox = new SyntaxHighlightingTextBox() {
                BorderThickness = new Thickness(0),
                AcceptsReturn = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFE, 0xF0)),
            };

            var sourceCodeBinding = new Binding("CurrentFileContent") {
                Mode = BindingMode.OneWay
            };
            textBox.SetBinding(SyntaxHighlightingTextBox.SourceCodeProperty, sourceCodeBinding);

            var sourceLanguageBinding = new Binding("CurrentFileLanguage") {
                Mode = BindingMode.OneWay,
                FallbackValue = SourceLanguageType.Plain
            };
            textBox.SetBinding(SyntaxHighlightingTextBox.SourceLanguageProperty, sourceLanguageBinding);

            var fileCounterBinding = new Binding("CurrentFileCounter") {
                Mode = BindingMode.OneWay
            };
            textBox.SetBinding(SyntaxHighlightingTextBox.SourceCounterProperty, fileCounterBinding);

            return textBox;
        }

        private void OnPublishButtonClick(object sender, RoutedEventArgs e) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                MessageBox.Show(
                    PackageExplorer.Resources.Resources.NoNetworkConnection,
                    MessageLevel.Warning);
                return;
            }

            var viewModel = (PackageViewModel)DataContext;

            if (!viewModel.IsValid) {
                MessageBox.Show(
                    PackageExplorer.Resources.Resources.PackageHasNoFile,
                    MessageLevel.Warning);
                return;
            }

            string storedKey = SettingsManager.ReadApiKeyFromSettingFile();
            var publishPackageViewModel = new PublishPackageViewModel(viewModel) {
                PublishKey = storedKey
            };

            var dialog = new PublishPackageWindow { Owner = this };
            dialog.DataContext = publishPackageViewModel;
            dialog.ShowDialog();

            string newKey = publishPackageViewModel.PublishKey;
            if (!String.IsNullOrEmpty(newKey)) {
                SettingsManager.WriteApiKeyToSettingFile(newKey);
            }
        }

        private void CanPublishToFeedCommand(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e) {
            var model = DataContext as PackageViewModel;
            bool canExecute = model == null ? false : !model.IsInEditMode;

            e.CanExecute = canExecute;
            e.Handled = true;
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs args) {
            ExecuteSaveCommand("Save");
            args.Handled = true;
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs args) {
            ExecuteSaveCommand("SaveAs");
            args.Handled = true;
        }

        private void ExecuteSaveCommand(object parameter) {
            var model = DataContext as PackageViewModel;
            if (model != null) {
                model.SaveCommand.Execute(parameter);
            }
        }

        private void CanExecuteSaveCommand(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e) {
            var model = DataContext as PackageViewModel;
            bool canExecute = model == null ? false : model.SaveCommand.CanExecute(e.Parameter);

            e.CanExecute = canExecute;
            e.Handled = true;
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var model = DataContext as PackageViewModel;
            if (model != null) {
                model.SelectedItem = PackagesTreeView.SelectedItem;
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
                    Owner = this
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    part.Rename(dialog.NewName);
                }
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
                    Owner = this
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    string newName = dialog.NewName;
                    folder.AddFolder(newName);
                }
            }
        }

        private void OnAddNewFolder2Click(object sender, RoutedEventArgs e) {
            var folder = (DataContext as PackageViewModel).RootFolder;

            if (folder != null) {
                var dialog = new RenameWindow {
                    NewName = "NewFolder",
                    Owner = this
                };
                bool? result = dialog.ShowDialog();
                if (result ?? false) {
                    string newName = dialog.NewName;
                    folder.AddFolder(newName);
                }
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (DataContext == null) {
                return;
            }

            var rootFolder = (DataContext as PackageViewModel).RootFolder;
            string subFolder = (string)e.Parameter;
            rootFolder.AddFolder(subFolder);
            e.Handled = true;
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
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

        private void CloseMenuItem_Click(object sender, ExecutedRoutedEventArgs e) {
            bool isCanceled = AskToSaveCurrentFile();
            if (isCanceled) {
                return;
            }

            DataContext = null;
        }

        private void CanExecuteCloseCommand(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = DataContext != null;
            e.Handled = true;
        }

        private void OnExportMenuItem_Click(object sender, RoutedEventArgs e) {
            BrowseForFolder dialog = new BrowseForFolder();
            string rootPath = dialog.SelectFolder(
                "Choose a folder to export package to:",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                new System.Windows.Interop.WindowInteropHelper(this).Handle);

            if (!String.IsNullOrEmpty(rootPath)) {
                var model = (PackageViewModel)DataContext;
                if (model != null) {
                    try {
                        model.Export(rootPath);
                        MessageBox.Show("The package has been exported successfully.", MessageLevel.Information);
                    }
                    catch (Exception ex) {
                        MessageBox.Show(ex.Message, MessageLevel.Error);
                    }
                }
            }

            e.Handled = true;
        }

        private void RecentFileMenuItem_Click(object sender, RoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            MenuItem menuItem = (MenuItem)sender;
            var mruItem = (MruItem)menuItem.DataContext;
            if (mruItem == null) {
                _mruManager.Clear();
            }
            else {
                if (mruItem.PackageType == PackageType.LocalPackage) {
                    OpenLocalPackage(mruItem.Path);
                }
                else {
                    DownloadAndOpenDataServicePackage(mruItem);
                }
            }
        }

        private void DownloadAndOpenDataServicePackage(MruItem item) {
            Uri downloadUrl;
            if (Uri.TryCreate(item.Path, UriKind.Absolute, out downloadUrl)) {
                var progressWindow = new DownloadProgressWindow(downloadUrl, item.PackageName) { Owner = this };
                var result = progressWindow.ShowDialog();
                if (result ?? false) {
                    ZipPackage package = new ZipPackage(progressWindow.DownloadedFilePath);
                    LoadPackage(package, item.Path, PackageType.DataServicePackage);
                }
            }
        }
    }
}