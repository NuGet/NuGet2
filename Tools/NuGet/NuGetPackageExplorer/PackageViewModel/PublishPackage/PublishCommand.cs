using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class PublishCommand : ICommand {

        private PublishPackageViewModel _viewModel;

        public PublishCommand(PublishPackageViewModel viewModel) {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            _viewModel.PushPackage();
        }

        internal void RaiseCanExecuteChanged() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}