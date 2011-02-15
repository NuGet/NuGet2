using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class SortCommand : ICommand {

        private PackageChooserViewModel _viewModel;

        public SortCommand(PackageChooserViewModel viewModel) {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            string column = (string) parameter;
            _viewModel.Sort(column);
        }
    }
}
