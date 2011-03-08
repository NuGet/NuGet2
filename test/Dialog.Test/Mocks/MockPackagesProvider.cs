using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;
using NuGetConsole;

namespace NuGet.Dialog.Test {
    internal class MockPackagesProvider : PackagesProviderBase {

        public MockPackagesProvider() 
            : this(new Mock<IVsPackageManager>().Object, new Mock<IProjectManager>().Object) {
        }

        public MockPackagesProvider(IVsPackageManager packageManager, IProjectManager projectManager)
            : base(new Mock<Project>().Object, projectManager, new ResourceDictionary(),
                new ProviderServices(new Mock<ILicenseWindowOpener>().Object, new Mock<IProgressWindowOpener>().Object, new Mock<IScriptExecutor>().Object, new MockOutputConsoleProvider()),
                new Mock<IProgressProvider>().Object) {
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
