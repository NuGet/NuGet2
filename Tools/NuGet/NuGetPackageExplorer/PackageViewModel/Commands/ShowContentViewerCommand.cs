using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class ShowContentViewerCommand : CommandBase, ICommand {
        public ShowContentViewerCommand(PackageViewModel viewmodel) : base(viewmodel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            ViewModel.ShowContentViewer = !ViewModel.ShowContentViewer;
        }
    }
}
