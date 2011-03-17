using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PackageExplorerViewModel.Types;
using System.ComponentModel.Composition;

namespace PackageExplorerViewModel {

    [Export(typeof(IPackageViewModelFactory))]
    public class PackageViewModelFactory : IPackageViewModelFactory {

        [Import]
        public IMessageBox MessageBoxService {
            get;
            set;
        }

        public PackageViewModel CreateViewModel(NuGet.IPackage package, string packageSource) {
            return new PackageViewModel(package, packageSource, MessageBoxService);
        }
    }
}
