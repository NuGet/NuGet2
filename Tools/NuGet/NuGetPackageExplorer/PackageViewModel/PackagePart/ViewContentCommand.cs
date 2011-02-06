using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : ICommand {

        private readonly PackageFile _file;
        private readonly IPackageViewModel _packageViewModel;

        public ViewContentCommand(IPackageViewModel packageViewModel, PackageFile file) {
            _file = file;
            _packageViewModel = packageViewModel;
        }

        public bool CanExecute(object parameter) {
            return !IsBinaryFile(_file.Name);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            if (_packageViewModel != null) {
                _packageViewModel.ShowFile(_file.Name, ReadFileContent(_file));
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