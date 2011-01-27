using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shell;
using Microsoft.Win32;
using NuGet;

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

        private void Button_Click(object sender, RoutedEventArgs e) {
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
                this.DataContext = package;
                this.Title = PackageExplorer.Resources.Resources.Dialog_Title + " - " + package.ToString();
                PackageName.Text = packagePath;
                PackagePartView.ItemsSource = PathToTreeConverter.Convert(package.GetFiles().ToList()).Children;
                CloseFileContent();

                AddToRecentJumpList(packagePath);
            }
        }

        private void AddToRecentJumpList(string path) {
            JumpList.SetJumpList(Application.Current, _jumpList);

            var jumpPath = new JumpPath { Path = path };
            JumpList.AddToRecentCategory(jumpPath);

            _jumpList.Apply();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e) {
            var link = (Hyperlink)sender;
            UriHelper.OpenExternalLink(link.NavigateUri);
        }

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

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = (MenuItem)sender;
            PackageFile file = mi.DataContext as PackageFile;
            if (file != null) {
                SaveFile(file);
            }
        }

        private void SaveFile(PackageFile file) {
            SaveFileDialog dialog = new SaveFileDialog() {
                OverwritePrompt = true,
                Title = "Save " + file.Name,
                Filter = "All files (*.*)|*.*",
                FileName = file.Name
            };

            bool? result = dialog.ShowDialog(this);
            if (result ?? false) {
                using (FileStream fileStream = File.OpenWrite(dialog.FileName)) {
                    CopyStream(file.GetStream(), fileStream);
                }
            }
        }

        private void CopyStream(Stream source, Stream target) {
            byte[] buffer = new byte[1024 * 4];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0) {
                target.Write(buffer, 0, count);
            }
        }

        private void ViewContent_Click(object sender, RoutedEventArgs e) {
            MenuItem mi = (MenuItem)sender;
            PackageFile file = mi.DataContext as PackageFile;
            if (file != null) {
                ShowFileContent(file);
            }
        }

        private static string[] BinaryFileExtensions = new string[] { 
            ".DLL", ".EXE", ".CHM", ".PDF", ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF", ".RTF", ".PDB"
        };

        private bool IsBinaryFile(string path) {
            // TODO: check for content type of the file here
            string extension = Path.GetExtension(path).ToUpper();
            return BinaryFileExtensions.Any(p => p.Equals(extension));
        }

        private void CloseFileContent() {
            FileContentBorder.Visibility = Visibility.Collapsed;
            ContentGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Auto);
            FileContent.Text = String.Empty;
            FileContentBorder.Header = String.Empty;
        }

        private void ShowFileContent(PackageFile file) {
            if (IsBinaryFile(file.Name)) {
                MessageBox.Show("Unable to show binary files.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else {
                ContentGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                FileContentBorder.Visibility = Visibility.Visible;
                FileContentBorder.Header = file.Name;
                FileContent.Text = LoadFileContent(file);
            }
        }

        private string LoadFileContent(PackageFile file) {
            using (StreamReader reader = new StreamReader(file.GetStream())) {
                return reader.ReadToEnd();
            }
        }
    }
}