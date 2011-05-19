using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test {
    internal class MockPackagesProvider : PackagesProviderBase {

        public MockPackagesProvider() 
            : this(new Mock<IPackageRepository>().Object, new Mock<IVsPackageManager>().Object) {
        }

        public MockPackagesProvider(IPackageRepository localRepository, IVsPackageManager packageManagerr)
            : base(
                localRepository,
                new ResourceDictionary(),
                new ProviderServices(new Mock<ILicenseWindowOpener>().Object, new Mock<IProgressWindowOpener>().Object, new Mock<IScriptExecutor>().Object, new MockOutputConsoleProvider(), new Mock<IProjectSelectorService>().Object),
                new Mock<IProgressProvider>().Object,
                new Mock<ISolutionManager>().Object) {
        }

        public override IVsExtension CreateExtension(NuGet.IPackage package) {
            return new PackageItem(this, package);
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
