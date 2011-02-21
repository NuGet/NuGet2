using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : CommandBase, ICommand {

        public ViewContentCommand(PackageViewModel packageViewModel) : base(packageViewModel) {
        }

        public bool CanExecute(object parameter) {
            //var file = parameter as PackageFile;
            //return file == null ? false : !IsBinaryFile(file.Name);
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) {
            var file = parameter as PackageFile;
            if (IsBinaryFile(file.Name)) {
                ShowBinaryFile(file);
            }
            else {
                ViewModel.ShowFile(file.Name, ReadFileContent(file));
            }
        }

        private void ShowBinaryFile(PackageFile file) {
            //// copy to temporary file
            //// create package in the temprary file first in case the operation fails which would
            //// override existing file with a 0-byte file.
            //string tempFileName = Path.Combine(Path.GetTempPath(), file.Name);

            //using (Stream tempFileStream = File.Create(tempFileName)) {
            //    file.GetStream().CopyTo(tempFileStream);
            //}

            //Process.Start();
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