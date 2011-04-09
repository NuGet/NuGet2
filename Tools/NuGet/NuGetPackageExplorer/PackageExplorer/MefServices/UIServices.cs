using System.ComponentModel.Composition;
using Microsoft.Win32;
using PackageExplorerViewModel.Types;

namespace PackageExplorer {

    [Export(typeof(IUIServices))]
    internal class UIServices : IUIServices {

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
    }
}