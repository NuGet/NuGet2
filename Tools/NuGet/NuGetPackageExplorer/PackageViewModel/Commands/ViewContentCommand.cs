using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : CommandBase, ICommand {

        public ViewContentCommand(PackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            if ("Hide".Equals(parameter)) {
                ViewModel.ShowContentViewer = false;
            }
            else {
                var file = parameter as PackageFile;
                if (file != null) {
                    const string UnsupportedMessage = "*** The format of this file is not supported. ***";
                    string content = IsBinaryFile(file.Name) ? UnsupportedMessage : ReadFileContent(file);
                    ViewModel.ShowFile(file.Name, content);
                }
            }
        }

        private string ReadFileContent(PackageFile file) {
            using (StreamReader reader = new StreamReader(file.GetStream())) {
                return reader.ReadToEnd();
            }
        }

        private static string[] BinaryFileExtensions = new string[] { 
            ".DLL", ".EXE", ".CHM", ".PDF", ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF", ".RTF", ".PDB", ".ZIP", ".XAP", ".VSIX", ".NUPKG"
        };

        private bool IsBinaryFile(string path) {
            // TODO: check for content type of the file here
            string extension = Path.GetExtension(path).ToUpper();
            return BinaryFileExtensions.Any(p => p.Equals(extension));
        }
    }
}