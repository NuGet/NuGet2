using System;
using System.Windows.Data;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class EditPackageCommand : CommandBase, ICommand {

        public EditPackageCommand(IPackageViewModel viewModel) : base(viewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            var bindingGroup = parameter as BindingGroup;
            if (bindingGroup != null) {
                bindingGroup.BeginEdit();
            }

            ViewModel.BegingEdit();
        }
    }
}