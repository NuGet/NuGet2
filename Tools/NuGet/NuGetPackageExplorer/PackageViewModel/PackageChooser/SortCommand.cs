using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class SortCommand : ICommand {

        private PackageChooserViewModel _viewModel;

        public SortCommand(PackageChooserViewModel viewModel) {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            return _viewModel.TotalPackageCount > 0;
        }

        public event EventHandler CanExecuteChanged;

        internal void RaiseCanExecuteEvent() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public void Execute(object parameter) {
            string column = (string) parameter;
            if (column != "Version") {
                _viewModel.Sort(column);
            }
        }
    }
}
