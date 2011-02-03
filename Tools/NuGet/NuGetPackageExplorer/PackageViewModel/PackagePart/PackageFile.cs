using System;
using System.IO;
using NuGet;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    public class PackageFile : PackagePart {

        private readonly IPackageFile _file;
        private ICommand _viewCommand;
        private ICommand _saveCommand;

        public IShowFileContentHandler ShowFileContentHandler { get; set; }

        public PackageFile(IPackageFile file, string name) : base(name) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            _file = file;
        }

        public ICommand ViewCommand {
            get {
                if (_viewCommand == null) {
                    _viewCommand = new ViewContentCommand(_file, Name, ShowFileContentHandler);
                }

                return _viewCommand;
            }
        }

        public ICommand SaveCommand {
            get {
                if (_saveCommand == null) {
                    _saveCommand = new SaveContentCommand(_file);
                }

                return _saveCommand;
            }
        }
    }
}