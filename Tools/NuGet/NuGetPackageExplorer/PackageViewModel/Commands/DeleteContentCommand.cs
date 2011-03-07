using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class DeleteContentCommand : PackagePartCommandBase {
        public DeleteContentCommand(PackageViewModel viewModel) : base(viewModel) {
        }

        public override bool CanExecute(object parameter) {
            return parameter is PackagePart;
        }

        public override void Execute(object parameter) {
            var file = parameter as PackagePart;
            if (file != null) {
                file.Delete();
            }
        }
    }
}