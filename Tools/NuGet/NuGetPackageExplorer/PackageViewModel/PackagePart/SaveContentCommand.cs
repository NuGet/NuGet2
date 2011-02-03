using System;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class SaveContentCommand : ICommand {

        private PackageFile _file;
        private IPackageViewModel _packageViewModel;

        public SaveContentCommand(PackageFile file, IPackageViewModel packageViewModel) {
            _file = file;
            _packageViewModel = packageViewModel;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            SaveFile(_file);
        }

        private void SaveFile(PackageFile file) {
            string selectedFileName;
            if (_packageViewModel.OpenSaveFileDialog(file.Name, out selectedFileName))
            {
                using (FileStream fileStream = File.OpenWrite(selectedFileName))
                {
                    CopyStream(file.GetStream(), fileStream);
                }
            }
        }

        private void CopyStream(Stream source, Stream target) {
            byte[] buffer = new byte[1024 * 4];     // 4K
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0) {
                target.Write(buffer, 0, count);
            }
        }
    }
}