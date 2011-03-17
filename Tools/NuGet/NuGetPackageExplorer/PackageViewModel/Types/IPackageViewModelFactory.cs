using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace PackageExplorerViewModel.Types {
    public interface IPackageViewModelFactory {
        PackageViewModel CreateViewModel(IPackage package, string packageSource);
    }
}
