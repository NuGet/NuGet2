using System.ComponentModel.Composition;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {

    [Export(typeof(IPackageViewModelFactory))]
    public class PackageViewModelFactory : IPackageViewModelFactory {

        [Import]
        public IMessageBox MessageBoxService {
            get;
            set;
        }

        [Import]
        public IMruManager MruManager {
            get;
            set;
        }

        public PackageViewModel CreateViewModel(NuGet.IPackage package, string packageSource) {
            return new PackageViewModel(package, packageSource, MessageBoxService, MruManager);
        }
    }
}
