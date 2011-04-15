using System;
using System.Windows.Data;
using System.Windows.Input;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {
    internal class CancelEditCommand : CommandBase, ICommand {

        public CancelEditCommand(PackageViewModel packageViewModel)
            : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged {
            add { }
            remove { }
        }

        public void Execute(object parameter) {
            var bindingGroup = parameter as IBindingGroup;
            if (bindingGroup != null) {
                bindingGroup.CancelEdit();
            }

            ViewModel.CancelEdit();
        }
    }
}