using System;
using System.Diagnostics;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class NavigateCommand : ICommand {

        private PackageChooserViewModel _viewModel;

        public NavigateCommand(PackageChooserViewModel viewModel) {
            Debug.Assert(viewModel != null);
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            string action = (string)parameter;
            switch (action) {
                case "First":
                    return CanMoveFirst();

                case "Previous":
                    return CanMovePrevious();

                case "Next":
                    return CanMoveNext();

                case "Last":
                    return CanMoveLast();
                        
                default:
                    throw new ArgumentOutOfRangeException("parameter");
            }
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            string action = (string)parameter;
            switch (action) {
                case "First":
                    MoveFirst();
                    break;

                case "Previous":
                    MovePrevious();
                    break;

                case "Next":
                    MoveNext();
                    break;

                case "Last":
                    MoveLast();
                    break;
            }
        }

        public void OnCanExecuteChanged() {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        private void MoveLast() {
            _viewModel.LoadPage(_viewModel.TotalPage - 1);
        }

        private void MoveNext() {
            _viewModel.LoadPage(_viewModel.CurrentPage + 1);
        }

        private void MovePrevious() {
            _viewModel.LoadPage(_viewModel.CurrentPage - 1);
        }

        private void MoveFirst() {
            _viewModel.LoadPage(0);
        }

        private bool CanMoveLast() {
            return _viewModel.CurrentPage < _viewModel.TotalPage - 1;
        }

        private bool CanMoveNext() {
            return _viewModel.CurrentPage  < _viewModel.TotalPage - 1;
        }

        private bool CanMovePrevious() {
            return _viewModel.CurrentPage > 0;
        }

        private bool CanMoveFirst() {
            return _viewModel.CurrentPage > 0;
        }
    }
}