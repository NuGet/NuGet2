using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using NuGet;

namespace PackageExplorerViewModel {
    public class PackageFile : PackagePart, IPackageFile {

        private readonly IPackageFile _file;

        public PackageFile(IPackageFile file, string name, PackageViewModel viewModel)
            : this(file, name, null, viewModel) {
        }

        public PackageFile(IPackageFile file, string name, PackageFolder parent)
            : this(file, name, parent, parent.PackageViewModel) {
        }

        private PackageFile(IPackageFile file, string name, PackageFolder parent, PackageViewModel viewModel)
            : base(name, parent, viewModel) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            _file = file;
        }

        public override IEnumerable<IPackageFile> GetFiles() {
            yield return this;
        }

        public Stream GetStream() {
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

        public ICommand OpenWithCommand {
            get { return PackageViewModel.OpenWithContentFileCommand; }
        }

        public override void Export(string rootPath) {
            string fullPath = System.IO.Path.Combine(rootPath, Path);
            if (File.Exists(fullPath)) {
                bool confirmed = PackageViewModel.UIServices.Confirm(
                    String.Format(CultureInfo.CurrentCulture, Resources.ConfirmToReplaceFile, fullPath)
                );
                if (!confirmed) {
                    return;
                }
            }

            using (var stream = File.Create(fullPath)) {
                GetStream().CopyTo(stream);
            }
        }
    }
}