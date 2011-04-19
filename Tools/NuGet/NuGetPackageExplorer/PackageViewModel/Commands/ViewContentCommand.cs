using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {
    internal class ViewContentCommand : CommandBase, ICommand {

        public ViewContentCommand(PackageViewModel packageViewModel)
            : base(packageViewModel) {
        }

        public event EventHandler CanExecuteChanged = delegate { };

        public bool CanExecute(object parameter) {
            return ViewModel.SelectedItem is PackageFile;
        }

        public void RaiseCanExecuteChanged() {
            CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter) {
            if ("Hide".Equals(parameter)) {
                ViewModel.ShowContentViewer = false;
            }
            else {
                var file = (parameter ?? ViewModel.SelectedItem) as PackageFile;
                if (file != null) {
                    ShowFile(file);
                }
            }
        }

        private void ShowFile(PackageFile file) {
            bool isBinary = IsBinaryFile(file.Name);
            long size;
            string content;

            if (isBinary) {
                content = Resources.UnsupportedFormatMessage;
                using (Stream stream = file.GetStream()) {
                    size = stream.Length;
                }
            }
            else {
                content = ReadFileContent(file, out size);
            }

            var fileInfo = new FileContentInfo(
                file,
                file.Path,
                content,
                !isBinary,
                size,
                DetermineLanguage(file.Name));

            ViewModel.ShowFile(fileInfo);
        }

        private static string ReadFileContent(PackageFile file, out long size) {
            const int MaxLengthToOpen = 10 * 1024;      // limit to 10K 
            const int BufferSize = 2 * 1024;
            char[] buffer = new char[BufferSize];       // read 2K at a time

            StringBuilder sb = new StringBuilder();
            Stream stream = file.GetStream();
            size = stream.Length;
            using (StreamReader reader = new StreamReader(stream)) {
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
            ".DLL", ".EXE", ".CHM", ".PDF", ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF", ".RTF", ".PDB", ".ZIP", ".RAR", ".XAP", ".VSIX", ".NUPKG", ".SNK", ".PFX", ".ICO"
        };

        private static bool IsBinaryFile(string path) {
            // TODO: check for content type of the file here
            string extension = Path.GetExtension(path).ToUpper(CultureInfo.InvariantCulture);
            return String.IsNullOrEmpty(extension) || BinaryFileExtensions.Any(p => p.Equals(extension));
        }

        private static SourceLanguageType DetermineLanguage(string name) {
            string extension = Path.GetExtension(name).ToUpperInvariant();

            // if the extension is .pp or .transform, it is NuGet transform files.
            // in which case, we strip out this extension and examine the real extension instead
            if (extension == ".PP" || extension == ".TRANSFORM") {
                name = Path.GetFileNameWithoutExtension(name);
                extension = Path.GetExtension(name).ToUpperInvariant();
            }

            switch (extension) {
                case ".ASAX":
                    return SourceLanguageType.Asax;

                case ".ASHX":
                    return SourceLanguageType.Ashx;

                case ".ASPX":
                    return SourceLanguageType.Aspx;

                case ".CS":
                    return SourceLanguageType.CSharp;

                case ".CPP":
                    return SourceLanguageType.Cpp;

                case ".CSS":
                    return SourceLanguageType.Css;

                case ".HTML":
                case ".HTM":
                    return SourceLanguageType.Html;

                case ".JS":
                    return SourceLanguageType.JavaScript;

                case ".PHP":
                    return SourceLanguageType.Php;

                case ".PS1":
                case ".PSM1":
                    return SourceLanguageType.PowerShell;

                case ".SQL":
                    return SourceLanguageType.Sql;

                case ".VB":
                    return SourceLanguageType.VisualBasic;

                case ".XAML":
                    return SourceLanguageType.Xaml;

                case ".XML":
                case ".XSD":
                case ".CONFIG":
                case ".PS1XML":
                    return SourceLanguageType.Xml;

                default:
                    return SourceLanguageType.Text;
            }
        }
    }
}