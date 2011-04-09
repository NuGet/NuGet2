
namespace PackageExplorerViewModel.Types {
    public interface IUIServices {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenFileDialog(string title, string filter, out string selectedFileName);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames);
    }
}
