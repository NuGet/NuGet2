using Microsoft.Win32;

namespace PackageExplorerViewModel {

    internal class AddContentFileCommand : PackagePartCommandBase {

        public AddContentFileCommand(PackageViewModel viewModel) : base(viewModel) {
        }

        public override bool CanExecute(object parameter) {
            return parameter == null || parameter is PackageFolder;
        }

        public override void Execute(object parameter) {
            PackageFolder folder = parameter as PackageFolder;
            if (folder != null) {
                AddExistingFileToFolder(folder);
            }
            else {
                AddExistingFileToFolder(ViewModel.RootFolder);
            }
        }

        private void AddExistingFileToFolder(PackageFolder folder) {

            string[] selectedFiles;
            bool result = ViewModel.UIServices.OpenMultipleFilesDialog(
                "Select Files",
                "All files (*.*)|*.*",
                out selectedFiles);

            if (result) {
                foreach (string file in selectedFiles) {
                    folder.AddFile(file);
                }
            }
        }
    }
}