using System;
using System.Windows.Data;
using System.Windows.Input;

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
            var bindingGroup = parameter as BindingGroup;
            if (bindingGroup != null) {
                bindingGroup.CancelEdit();
            }

            ViewModel.CancelEdit();
        }
    }
}
