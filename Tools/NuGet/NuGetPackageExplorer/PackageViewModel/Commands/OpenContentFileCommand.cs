using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class OpenContentFileCommand : CommandBase, ICommand {

        public OpenContentFileCommand(PackageViewModel packageViewModel)
            : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            var file = parameter as PackageFile;
            OpenFileInShell(file);
        }

        private void OpenFileInShell(PackageFile file) {
            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            string tempFileName = Path.Combine(Path.GetTempPath(), file.Name);

            using (Stream tempFileStream = File.Create(tempFileName)) {
                file.GetStream().CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName)) {
                Process.Start("explorer.exe", tempFileName);
            }
        }
    }
}