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

        [Import]
        public IMruPackageSourceManager MruPackageSourceManager {
            get;
            set;
        }

        [Import]
        public IUIServices UIServices {
            get;
            set;
        }

        public PackageViewModel CreateViewModel(NuGet.IPackage package, string packageSource) {
            return new PackageViewModel(package, packageSource, MessageBoxService, MruManager, UIServices);
        }

        public PackageChooserViewModel CreatePackageChooserViewModel() {
            return new PackageChooserViewModel(MruPackageSourceManager);
        }
    }
}
