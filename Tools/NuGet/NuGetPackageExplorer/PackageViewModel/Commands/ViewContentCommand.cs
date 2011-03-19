using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;

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
                    string content = IsBinaryFile(file.Name) ? Resources.UnsupportedFormatMessage : ReadFileContent(file);
                    ViewModel.ShowFile(file.Path, content);
                }
            }
        }

        private static string ReadFileContent(PackageFile file) {
            const int MaxLengthToOpen = 10 * 1024;      // limit to 10K 
            const int BufferSize = 2 * 1024;
            char[] buffer = new char[BufferSize];       // read 2K at a time

            StringBuilder sb = new StringBuilder();
            using (StreamReader reader = new StreamReader(file.GetStream())) {
                while (sb.Length < MaxLengthToOpen) {
                    int bytesRead = reader.Read(buffer, 0, BufferSize);
                    if (bytesRead == 0) {
                        break;
                    }
                    else {
                        sb.Append(new string(buffer, 0, bytesRead));
                    }
                }

                // if not reaching the end of the stream yet, append the text "Truncating..."
                if (reader.Peek() > -1) {
                    // continue reading the rest of the current line to avoid dangling line
                    sb.AppendLine(reader.ReadLine());

                    if (reader.Peek() > -1) {
                        sb.AppendLine().AppendLine("*** The rest of the content is truncated. ***");
                    }
                }
            }

            return sb.ToString();
        }

        private static string[] BinaryFileExtensions = new string[] { 
            ".DLL", ".EXE", ".CHM", ".PDF", ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF", ".RTF", ".PDB", ".ZIP", ".XAP", ".VSIX", ".NUPKG", ".SNK", ".PFX", ".ICO"
        };

        private static bool IsBinaryFile(string path) {
            // TODO: check for content type of the file here
            string extension = Path.GetExtension(path).ToUpper(CultureInfo.InvariantCulture);
            return String.IsNullOrEmpty(extension) || BinaryFileExtensions.Any(p => p.Equals(extension));
        }
    }
}