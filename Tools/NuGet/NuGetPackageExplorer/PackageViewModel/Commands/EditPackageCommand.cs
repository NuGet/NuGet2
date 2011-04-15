using System;
using System.Windows.Data;
using System.Windows.Input;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {
    internal class EditPackageCommand : CommandBase, ICommand {

        public EditPackageCommand(PackageViewModel viewModel) : base(viewModel) {
            viewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnPropertyChanged);
        }

        void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals("IsInEditMode")) {
                if (CanExecuteChanged != null) {
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
        }

        public bool CanExecute(object parameter) {
            return !ViewModel.IsInEditMode;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            var bindingGroup = parameter as IBindingGroup;
            if (bindingGroup != null) {
                bindingGroup.BeginEdit();
            }

            ViewModel.BeginEdit();
        }
    }
}