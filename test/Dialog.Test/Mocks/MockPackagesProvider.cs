using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;
using NuGet.Dialog.PackageManagerUI;

namespace NuGet.Dialog.Test {
    internal class MockPackagesProvider : PackagesProviderBase {

        public MockPackagesProvider() 
            : this(new Mock<IVsPackageManager>().Object, new Mock<IProjectManager>().Object) {
        }

        public MockPackagesProvider(IVsPackageManager packageManager, IProjectManager projectManager)
            : base(null, projectManager, new ResourceDictionary(), new Mock<IProgressWindowOpener>().Object, null) {
        }

        public override IVsExtension CreateExtension(NuGet.IPackage package) {
            return new PackageItem(this, package, null);
        }

        public override bool CanExecute(PackageItem item) {
            return true;
        }

        public override void Execute(PackageItem item) {
        }

        public override string Name {
            get { return "Mock Provider"; }
        }
    }
}
