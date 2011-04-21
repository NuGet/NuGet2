using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.Win32;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {

    [Export(typeof(IUIServices))]
    internal class UIServices : IUIServices {

        [Import]
        public Lazy<MainWindow> Window { get; set; }

        public bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath) {
            var dialog = new SaveFileDialog() {
                OverwritePrompt = true,
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFilePath = dialog.FileName;
                return true;
            }
            else {
                selectedFilePath = null;
                return false;
            }
        }

        public bool OpenFileDialog(string title, string filter, out string selectedFileName) {
            var dialog = new OpenFileDialog() {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = false,
                ValidateNames = true,
                Filter = filter
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFileName = dialog.FileName;
                return true;
            }
            else {
                selectedFileName = null;
                return false;
            }
        }

        public bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames) {
            var dialog = new OpenFileDialog() {
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true,
                FilterIndex = 0,
                Multiselect = true,
                ValidateNames = true,
                Filter = filter
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                selectedFileNames = dialog.FileNames;
                return true;
            }
            else {
                selectedFileNames = null;
                return false;
            }
        }

        public bool Confirm(string message) {
            return Confirm(message, false);
        }

        public bool Confirm(string message, bool isWarning) {
            MessageBoxResult result = MessageBox.Show(
                Window.Value,
                message,
                Resources.Resources.Dialog_Title,
                MessageBoxButton.YesNo,
                isWarning ? MessageBoxImage.Warning : MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public bool? ConfirmWithCancel(string message) {
            MessageBoxResult result = MessageBox.Show(
                Window.Value,
                message,
                Resources.Resources.Dialog_Title,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) {
                return null;
            }
            else {
                return result == MessageBoxResult.Yes;
            }
        }

        public void Show(string message, MessageLevel messageLevel) {
            MessageBoxImage image;
            switch (messageLevel) {
                case MessageLevel.Error:
                    image = MessageBoxImage.Error;
                    break;

                case MessageLevel.Information:
                    image = MessageBoxImage.Information;
                    break;

                case MessageLevel.Warning:
                    image = MessageBoxImage.Warning;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("messageLevel");
            }

            MessageBox.Show(
                Window.Value,
                message,
                Resources.Resources.Dialog_Title,
                MessageBoxButton.OK,
                image);
        }

        public bool OpenRenameDialog(string currentName, out string newName) {
            var dialog = new RenameWindow {
                NewName = currentName,
                Owner = Window.Value
            };
            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                newName = dialog.NewName;
                return true;
            }
            else {
                newName = null;
                return false;
            }
        }

        public bool OpenPublishDialog(object viewModel) {
            var dialog = new PublishPackageWindow { 
                Owner = Window.Value,
                DataContext = viewModel
            };
            var result = dialog.ShowDialog();
            return result ?? false;
        }

        public bool OpenFolderDialog(string title, string initialPath, out string selectedPath) {
            BrowseForFolder dialog = new BrowseForFolder();
            selectedPath = dialog.SelectFolder(
                title,
                initialPath,
                new System.Windows.Interop.WindowInteropHelper(Window.Value).Handle);
            return !String.IsNullOrEmpty(selectedPath);
        }
    }
}