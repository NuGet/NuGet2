using System;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using NuGet;
using PackageExplorer.Properties;
using PackageExplorerViewModel;
using StringResources = PackageExplorer.Resources.Resources;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            try {
                LoadSettings();
            }
            catch (Exception) { }

            BuildStatusItem.Content = "Build " + typeof(MainWindow).Assembly.GetName().Version.ToString();
        }

        internal void OpenLocalPackage(string packagePath) {
            ZipPackage package = null;
            try {
                package = new ZipPackage(packagePath);
            }
            catch (Exception ex) {
                MessageBox.Show(
                    ex.Message, 
                    StringResources.Dialog_Title, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            if (package != null) {
                LoadPackage(package, packagePath);
            }
        }

        private void LoadPackage(IPackage package, string packagePath) {
            if (package != null) {
                DataContext = new PackageViewModel(package, packagePath);
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) {
            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = Constants.PackageExtension,
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
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Network connection is not detected.", "Network error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool canceled = AskToSaveCurrentFile();
            if (canceled) {
                return;
            }

            var dialog = new PackageChooserDialog() {
                Owner = this,
                FontSize = this.FontSize
            };
            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                if (dialog.SelectedPackage != null) {

                    var progressWindow = new DownloadProgressWindow(dialog.SelectedPackage) {
                        Owner = this
                    };

                    result = progressWindow.ShowDialog();
                    if (result ?? false) {
                        LoadPackage(dialog.SelectedPackage, dialog.SelectedPackage.DownloadUrl.ToString());
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
                    if (firstFile.EndsWith(Constants.PackageExtension)) {
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

                    string firstFile = filenames.FirstOrDefault(
                        f => f.EndsWith(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase));

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

        #endregion

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            var link = (Hyperlink)sender;
            UriHelper.OpenExternalLink(link.NavigateUri);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(
                StringResources.Dialog_HelpAbout,
                StringResources.Dialog_Title, 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            bool isCanceled = AskToSaveCurrentFile();
            e.Cancel = isCanceled;
        }

        /// <summary>
        /// Asks user to save the current file before doing something (e.g. exit, open a new file)
        /// </summary>
        /// <returns>true if user cancels the impending action</returns>
        private bool AskToSaveCurrentFile() {
            
            if (HasUnsavedChanges) {

                var question = String.Format(
                    CultureInfo.CurrentCulture,
                    StringResources.Dialog_SaveQuestion,
                    System.IO.Path.GetFileName(PackageSourceItem.Content.ToString()));

                // if there is unsaved changes, ask user for confirmation
                var result = MessageBox.Show(
                    question,
                    PackageExplorer.Resources.Resources.Dialog_Title,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) {
                    return true;
                }

                if (result == MessageBoxResult.Yes) {
                    var saveCommand = SaveMenuItem.Command;
                    const string parameter = "Save";
                    if (saveCommand.CanExecute(parameter)) {
                        saveCommand.Execute(parameter);
                    }
                }
            }

            return false;
        }

        private bool HasUnsavedChanges {
            get {
                var viewModel = (PackageViewModel) DataContext;
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
            this.FontSize = size;

            // check the corresponding font size menu item 
            foreach (MenuItem child in FontSizeMenuItem.Items) {
                int value = Convert.ToInt32(child.Tag);
                child.IsChecked = value == size;
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            try {
                SaveSettings();
            }
            catch (Exception) { }
        }

        private void SaveSettings() {
            Settings settings = Properties.Settings.Default;

            settings.FontSize = (int)Math.Round(this.FontSize);
            settings.Left = this.Left;
            settings.Top = this.Top;
            settings.Width = this.Width;
            settings.Height = this.Height;
            settings.WindowState = this.WindowState.ToString();
        }

        private void LoadSettings() {
            Settings settings = Properties.Settings.Default;
            
            SetFontSize(settings.FontSize);

            double left = settings.Left;
            double top = settings.Top;

            if (left > 0 && top > 0) {
                this.Left = left;
                this.Top = top;
            }

            double width = settings.Width;
            double height = settings.Height;

            if (width > 0 && height > 0) {
                this.Width = width;
                this.Height = Height;
            }

            string windowState = settings.WindowState;
            if (!String.IsNullOrEmpty(windowState)) {
                WindowState state;
                if (Enum.TryParse<WindowState>(windowState, out state) && state == WindowState.Maximized) {
                    this.WindowState = state;
                }
            }
        }

        private void ViewFileFormatItem_Click(object sender, RoutedEventArgs e) {
            Uri uri = new Uri("http://nuget.codeplex.com/documentation?title=Creating%20a%20Package");
            UriHelper.OpenExternalLink(uri);
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

        private void GroupBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var settings = Properties.Settings.Default;

            if ((bool)e.NewValue) {
                ContentGrid.RowDefinitions[2].Height = new GridLength(settings.ContentViewerPanelHeight, GridUnitType.Pixel);
            }
            else {
                settings.ContentViewerPanelHeight = ContentGrid.RowDefinitions[2].Height.Value;
                ContentGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs args)
        {
            ExecuteSaveCommand("Save");
            args.Handled = true;
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs args)
        {
            ExecuteSaveCommand("SaveAs");
            args.Handled = true;
        }

        private void ExecuteSaveCommand(object parameter)
        {
            var model = DataContext as PackageViewModel;
            if (model != null)
            {
                model.SaveCommand.Execute(parameter);
            }
        }

        private void CanExecuteSaveCommand(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            var model = DataContext as PackageViewModel;
            bool canExecute = model == null ? false : model.SaveCommand.CanExecute(e.Parameter);

            e.CanExecute = canExecute;
            e.Handled = true;
        }
    }
}