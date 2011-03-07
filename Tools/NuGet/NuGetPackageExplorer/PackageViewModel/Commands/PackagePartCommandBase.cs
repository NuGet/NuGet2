using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal abstract class PackagePartCommandBase : CommandBase, ICommand {

        public PackagePartCommandBase(PackageViewModel viewModel) : base(viewModel) {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "SelectedItem") {
                if (CanExecuteChanged != null) {
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
        }

        public abstract bool CanExecute(object parameter);

        public event EventHandler CanExecuteChanged;

        public abstract void Execute(object parameter);
    }
}
