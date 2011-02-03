using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : ICommand {

        private readonly IPackageFile _file;
        private readonly string _name;
        private readonly IShowFileContentHandler _showFileContentHandler;

        public ViewContentCommand(IPackageFile file, string name, IShowFileContentHandler showFileContentHandler) {
            _file = file;
            _name = name;
            _showFileContentHandler = showFileContentHandler;
        }

        public bool CanExecute(object parameter) {
            return !IsBinaryFile(_file.Path);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            if (_showFileContentHandler != null) {
                _showFileContentHandler.ShowFile(_name, ReadFileContent(_file));
            }
        }

        private string ReadFileContent(IPackageFile file) {
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