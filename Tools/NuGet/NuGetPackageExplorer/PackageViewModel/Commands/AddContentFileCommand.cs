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
            OpenFileDialog dialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "*.*",
                Multiselect = true,
                ValidateNames = true,
                Filter = "All files (*.*)|*.*"
            };

            bool? result = dialog.ShowDialog();
            if (result ?? false) {
                foreach (string file in dialog.FileNames) {
                    folder.AddFile(file);
                }
            }
        }
    }
}