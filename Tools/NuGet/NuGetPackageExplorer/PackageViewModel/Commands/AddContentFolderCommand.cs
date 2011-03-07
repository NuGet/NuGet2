using System;
using System.Windows.Input;
using System.Collections.Specialized;

namespace PackageExplorerViewModel {

    internal class AddContentFolderCommand : CommandBase, ICommand {

        public AddContentFolderCommand(PackageViewModel viewModel) : base(viewModel) {
            var notifyCollection = viewModel.RootFolder.Children as INotifyCollectionChanged;
            if (notifyCollection != null) {
                notifyCollection.CollectionChanged += OnRootFolderCollectionChanged;
            }
        }

        private void OnRootFolderCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (CanExecuteChanged != null) {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter) {
            if (parameter == null) {
                return false;
            }

            string folderName = (string)parameter;
            return !ViewModel.RootFolder.ContainsFolder(folderName);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            if (parameter == null) {
                return;
            }

            string folderName = (string)parameter;
            ViewModel.RootFolder.AddFolder(folderName);
        }
    }
}
