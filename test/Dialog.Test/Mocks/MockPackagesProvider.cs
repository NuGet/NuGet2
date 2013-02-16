using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Moq;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.Providers;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test
{
    internal class MockPackagesProvider : PackagesProviderBase
    {
        private IEnumerable<string> _supportedFrameworks;

        public MockPackagesProvider()
            : this(new Mock<IPackageRepository>().Object, new Mock<IVsPackageManager>().Object, Enumerable.Empty<string>())
        {
        }

        public MockPackagesProvider(IEnumerable<string> supportedFrameworks)
            : this(new Mock<IPackageRepository>().Object, new Mock<IVsPackageManager>().Object, supportedFrameworks)
        {
        }

        public MockPackagesProvider(IPackageRepository localRepository, IVsPackageManager packageManager)
            : this(localRepository, packageManager, Enumerable.Empty<string>())
        {
        }

        public MockPackagesProvider(IPackageRepository localRepository, IVsPackageManager packageManagerr, IEnumerable<string> supportedFrameworks)
            : base(localRepository, 
                   new ResourceDictionary(), 
                   new ProviderServices(
                       new Mock<IUserNotifierServices>().Object,
                       new Mock<IProgressWindowOpener>().Object,
                       new Mock<IProviderSettings>().Object,
                       new Mock<IUpdateAllUIService>().Object,
                       new Mock<IScriptExecutor>().Object,
                       new MockOutputConsoleProvider(),
                       new Mock<IVsCommonOperations>().Object),
                   new Mock<IProgressProvider>().Object, 
                   new Mock<ISolutionManager>().Object)
        {
            _supportedFrameworks = supportedFrameworks;
        }

        public override IEnumerable<string> SupportedFrameworks
        {
            get
            {
                return _supportedFrameworks;
            }
        }

        public override IVsExtension CreateExtension(NuGet.IPackage package)
        {
            return new PackageItem(this, package);
        }

        public override bool CanExecute(PackageItem item)
        {
            return true;
        }

        public override void Execute(PackageItem item)
        {
        }

        public override string Name
        {
            get { return "Mock Provider"; }
        }
    }
}
