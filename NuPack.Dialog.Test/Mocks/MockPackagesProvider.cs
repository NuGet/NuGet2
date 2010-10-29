using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test {
    internal class MockPackagesProvider : PackagesProviderBase {

        public MockPackagesProvider() 
            : this(new Mock<IVsPackageManager>().Object, new Mock<IProjectManager>().Object) {
        }

        public MockPackagesProvider(IVsPackageManager packageManager, IProjectManager projectManager)
            : base(packageManager, projectManager, new ResourceDictionary()) {
        }

        public override IVsExtension CreateExtension(NuGet.IPackage package) {
            return new PackageItem(this, package, null);
        }

        public override bool CanExecute(PackageItem item) {
            return true;
        }

        public override void Execute(PackageItem item, PackageManagerUI.ILicenseWindowOpener licenseWindowOpener) {
        }

        public override string Name {
            get { return "Mock Provider"; }
        }
    }
}
