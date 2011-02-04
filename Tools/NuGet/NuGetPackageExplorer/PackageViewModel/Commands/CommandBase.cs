using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;

namespace PackageExplorerViewModel {
    internal class CommandBase {

        protected CommandBase(IPackageViewModel viewModel, IPackage package) {
            this.ViewModel = viewModel;
            this.Package = package;
        }

        protected IPackageViewModel ViewModel { get; private set; }

        protected IPackage Package { get; private set; }
    }
}
