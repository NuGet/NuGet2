using System;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    public class PackageFile : PackagePart {

        private readonly IPackageFile _file;
        private ICommand _viewCommand;
        private ICommand _saveCommand;

        public IPackageViewModel PackageViewModel { get; set; }

        public PackageFile(IPackageFile file, string name) : base(name, file.Path) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            _file = file;
        }

        public Stream GetStream()
        {
            return _file.GetStream();
        }

        public ICommand ViewCommand {
            get {
                if (_viewCommand == null) {
                    _viewCommand = new ViewContentCommand(PackageViewModel, this);
                }

                return _viewCommand;
            }
        }

        public ICommand SaveCommand {
            get {
                if (_saveCommand == null) {
                    _saveCommand = new SaveContentCommand(PackageViewModel, this);
                }

                return _saveCommand;
            }
        }
    }
}