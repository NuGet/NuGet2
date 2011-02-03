using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Shell;
using Microsoft.Win32;
using NuGet;
using PackageExplorerViewModel;

namespace PackageExplorer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly JumpList _jumpList = new JumpList() { ShowRecentCategory = true };

        public MainWindow() {
            InitializeComponent();

            BuildStatusItem.Content = "Build " + typeof(MainWindow).Assembly.GetName().Version.ToString();
        }

        internal void LoadPackage(string packagePath) {
            ZipPackage package = null;
            try {
                package = new ZipPackage(packagePath);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (package != null) {
                DataContext = new PackageViewModel(package, packagePath);
                AddToRecentJumpList(packagePath);
            }
        }

        private void AddToRecentJumpList(string path) {
            JumpList.SetJumpList(Application.Current, _jumpList);

            var jumpPath = new JumpPath { Path = path };
            JumpList.AddToRecentCategory(jumpPath);

            _jumpList.Apply();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = Constants.PackageExtension,
                Multiselect = false,
                ValidateNames = true,
                Filter = "NuGet package file (*.nupkg)|*.nupkg"
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                LoadPackage(dialog.FileName);
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
                        LoadPackage(firstFile);
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

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e) {
            DataContext = null;
        }
    }
}