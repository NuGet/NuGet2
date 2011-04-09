
namespace PackageExplorerViewModel.Types {
    public interface IUIServices {
        bool OpenSaveFileDialog(string title, string defaultFileName, string filter, out string selectedFilePath);
        bool OpenFileDialog(string title, string filter, out string selectedFileName);
        bool OpenMultipleFilesDialog(string title, string filter, out string[] selectedFileNames);
    }
}
