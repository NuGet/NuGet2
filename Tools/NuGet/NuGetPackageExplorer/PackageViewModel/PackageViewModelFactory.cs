using System;
using System.ComponentModel.Composition;
using PackageExplorerViewModel.Types;

namespace PackageExplorerViewModel {

    [Export(typeof(IPackageViewModelFactory))]
    public class PackageViewModelFactory : IPackageViewModelFactory {

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

        [Import]
        public Lazy<IPackageEditorService> EditorService {
            get;
            set;
        }

        [Import]
        public ISettingsManager SettingsManager {
            get;
            set;
        }

        public PackageViewModel CreateViewModel(NuGet.IPackage package, string packageSource) {
            return new PackageViewModel(package, packageSource, MruManager, UIServices, EditorService.Value, SettingsManager);
        }

        public PackageChooserViewModel CreatePackageChooserViewModel() {
            return new PackageChooserViewModel(MruPackageSourceManager);
        }
    }
}
