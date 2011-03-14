using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Globalization;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : CommandBase, ICommand {

        public ViewContentCommand(PackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged {
            add { }
            remove { }
        }

        public void Execute(object parameter) {
            if ("Hide".Equals(parameter)) {
                ViewModel.ShowContentViewer = false;
            }
            else {
                var file = parameter as PackageFile;
                if (file != null) {
                    const string UnsupportedMessage = "*** The format of this file is not supported. ***";
                    string content = IsBinaryFile(file.Name) ? UnsupportedMessage : ReadFileContent(file);
                    ViewModel.ShowFile(file.Path, content);
                }
            }
        }

        private static string ReadFileContent(PackageFile file) {
            using (StreamReader reader = new StreamReader(file.GetStream())) {
                return reader.ReadToEnd();
            }
        }

        private static string[] BinaryFileExtensions = new string[] { 
            ".DLL", ".EXE", ".CHM", ".PDF", ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF", ".RTF", ".PDB", ".ZIP", ".XAP", ".VSIX", ".NUPKG"
        };

        private static bool IsBinaryFile(string path) {
            // TODO: check for content type of the file here
            string extension = Path.GetExtension(path).ToUpper(CultureInfo.InvariantCulture);
            return String.IsNullOrEmpty(extension) || BinaryFileExtensions.Any(p => p.Equals(extension));
        }
    }
}