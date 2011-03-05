using System;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    public class PackageFile : PackagePart {

        private readonly IPackageFile _file;

        public PackageViewModel PackageViewModel { get; set; }

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
            get { return PackageViewModel.ViewContentCommand; }
        }

        public ICommand SaveCommand {
            get { return PackageViewModel.SaveContentCommand; }
        }

        public ICommand OpenCommand {
            get { return PackageViewModel.OpenContentFileCommand; }
        }
    }
}