
namespace PackageExplorerViewModel.Types {

    public enum MessageLevel {
        Information,
        Warning,
        Error
    }

    public interface IUIServices {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenFileDialog(string title, string filter, out string selectedFileName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        bool OpenRenameDialog(string currentName, out string newName);

        bool OpenPublishDialog(object viewModel);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenFolderDialog(string title, string initialPath, out string selectedPath);

        bool Confirm(string message);
        bool Confirm(string message, bool isWarning);
        bool? ConfirmWithCancel(string message);
        void Show(string message, MessageLevel messageLevel);
    }
}
