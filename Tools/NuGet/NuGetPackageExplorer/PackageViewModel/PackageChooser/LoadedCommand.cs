using System;
using System.Windows.Input;

namespace PackageExplorerViewModel
{
    public class LoadedCommand : ICommand
    {

        private PackageChooserViewModel _viewModel;

        public LoadedCommand(PackageChooserViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _viewModel.LoadPackages();
        }
    }
}
