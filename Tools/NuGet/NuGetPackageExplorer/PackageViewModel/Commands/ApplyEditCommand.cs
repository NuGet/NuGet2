using System;
using System.Windows.Data;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class ApplyEditCommand : CommandBase, ICommand {

        public ApplyEditCommand(IPackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            var bindingGroup = parameter as BindingGroup;
            if (bindingGroup != null) {
                bindingGroup.CommitEdit();
            }

            ViewModel.CommitEdit();
        }
    }
}