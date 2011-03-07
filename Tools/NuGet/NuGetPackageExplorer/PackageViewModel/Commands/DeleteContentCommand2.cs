using System;
using System.Windows.Input;

namespace PackageExplorerViewModel {
    internal class DeleteContentCommand2 : PackagePartCommandBase {
        public DeleteContentCommand2(PackageViewModel viewModel)
            : base(viewModel) {
        }

        public override bool CanExecute(object parameter) {
            return true;
        }

        public override void Execute(object parameter) {
            var file = parameter as PackagePart;
            if (file != null) {
                file.Delete();
            }
        }
    }
}