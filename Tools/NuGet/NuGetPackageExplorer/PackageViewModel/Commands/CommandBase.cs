
namespace PackageExplorerViewModel {
    internal class CommandBase {

        protected CommandBase(IPackageViewModel viewModel) {
            this.ViewModel = viewModel;
        }

        protected IPackageViewModel ViewModel { get; private set; }
    }
}
