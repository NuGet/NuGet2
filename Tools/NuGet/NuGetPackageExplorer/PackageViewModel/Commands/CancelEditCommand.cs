using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class CancelEditCommand : CommandBase, ICommand {

        public CancelEditCommand(IPackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
             ViewModel.CancelEditMode();
        }
    }
}
