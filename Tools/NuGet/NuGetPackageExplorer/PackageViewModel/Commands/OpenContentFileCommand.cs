using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class OpenContentFileCommand : CommandBase, ICommand {

        private static string[] _executableScriptsExtensions = new string[] {
            ".BAS", ".BAT", ".CHM", ".COM", ".EXE", ".HTA", ".INF", ".JS", ".LNK", ".MSI", 
            ".OCX", ".PPT", ".REG", ".SCT", ".SHS", ".SYS", ".URL", ".VB", ".VBS", ".WSH", ".WSF"
        };

        public OpenContentFileCommand(PackageViewModel packageViewModel)
            : base(packageViewModel) {
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
            OpenFileInShell(file);
        }

        private void OpenFileInShell(PackageFile file) {
            if (IsExecutableScript(file.Name)) {
                bool confirm = ViewModel.UIServices.Confirm(Resources.OpenExecutableScriptWarning, isWarning: true);
                if (!confirm) {
                    return;
                }
            }

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

        private static bool IsExecutableScript(string fileName) {
            string extension = Path.GetExtension(fileName).ToUpperInvariant();
            return Array.IndexOf(_executableScriptsExtensions, extension) > -1;
        }
    }
}