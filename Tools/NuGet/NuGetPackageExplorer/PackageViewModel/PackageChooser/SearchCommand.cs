using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public sealed class SearchCommand : ICommand {
        
        private PackageChooserViewModel _viewModel;
        public SearchCommand(PackageChooserViewModel viewModel) {
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
            _viewModel.Search((string)parameter);
        }
    }
}
