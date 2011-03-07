
namespace PackageExplorerViewModel {

    internal class AddNewFolderCommand : PackagePartCommandBase {

        public AddNewFolderCommand(PackageViewModel viewModel)
            : base(viewModel) {
        }

        public override bool CanExecute(object parameter) {
            return parameter == null || parameter is PackageFolder;
        }

        public override void Execute(object parameter) {
            // this command do not apply to content file
            if (parameter != null && parameter is PackageFile) {
                return;
            }

            var folder = (parameter as PackageFolder) ?? ViewModel.RootFolder;
            folder.AddFolder("NewFolder");
        }
    }
}