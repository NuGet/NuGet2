using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace PackageExplorerViewModel {
    internal class CommandBase {

        protected CommandBase(IPackageViewModel viewModel) {
            this.ViewModel = viewModel;
        }

        protected IPackageViewModel ViewModel { get; private set; }
    }
}
