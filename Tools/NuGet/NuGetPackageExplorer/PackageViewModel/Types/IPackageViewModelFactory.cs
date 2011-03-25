using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IPackageViewModelFactory {
        PackageViewModel CreateViewModel(IPackage package, string packageSource);
        PackageChooserViewModel CreatePackageChooserViewModel();
    }
}
