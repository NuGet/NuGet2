using System;
using System.IO;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class SaveContentCommand : CommandBase, ICommand {

        public SaveContentCommand(PackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged {
            add { }
            remove { }
        }

        public void Execute(object parameter) {
            var file = parameter as PackageFile;
            if (file != null) {
                SaveFile(file);
            }
        }

        private void SaveFile(PackageFile file) {
            string selectedFileName;
            string title = "Save " + file.Name;
            string filter = "All files (*.*)|*.*";
            if (ViewModel.UIServices.OpenSaveFileDialog(title, file.Name, filter, out selectedFileName))
            {
                using (FileStream fileStream = File.OpenWrite(selectedFileName))
                {
                    file.GetStream().CopyTo(fileStream);
                }
            }
        }
    }
}