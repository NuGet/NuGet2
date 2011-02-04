using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class DisabledCommand : ICommand {
        public bool CanExecute(object parameter) {
            return false;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            throw new NotSupportedException();
        }
    }
}
