using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class SearchCommand : ICommand {

         private PackageChooserViewModel _viewModel;

        public SearchCommand(PackageChooserViewModel viewModel) {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            throw new NotImplementedException();
        }
    }
}
