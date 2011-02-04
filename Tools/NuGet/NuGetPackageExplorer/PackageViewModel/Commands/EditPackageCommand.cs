using System;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class EditPackageCommand : CommandBase, ICommand {

        public EditPackageCommand(IPackageViewModel viewModel, IPackage package) : base(viewModel, package) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            ViewModel.SetEditMode();
        }
    }
}