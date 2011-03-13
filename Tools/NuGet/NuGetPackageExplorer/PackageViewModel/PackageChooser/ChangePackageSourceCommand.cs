using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public sealed class ChangePackageSourceCommand : ICommand {

        private PackageChooserViewModel _viewModel;

        public ChangePackageSourceCommand(PackageChooserViewModel viewModel) {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged {
            add { }
            remove { }
        }

        public void Execute(object parameter) {
            string source = (string)parameter;
            if (!String.IsNullOrEmpty(source)) {
                _viewModel.ChangePackageSource(source);
            }
        }
    }
}
