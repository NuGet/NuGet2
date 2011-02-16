using System.ComponentModel.Composition;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public sealed class ProviderServices {
        public ILicenseWindowOpener LicenseWindow { get; private set; }
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IScriptExecutor ScriptExecutor { get; private set; }
        public IOutputConsoleProvider OutputConsoleProvider { get; private set; }

        [ImportingConstructor]
        public ProviderServices(
            ILicenseWindowOpener licenseWindowOpener,
            IProgressWindowOpener progressWindow,
            IScriptExecutor scriptExecutor,
            IOutputConsoleProvider outputConsoleProvider) {

            LicenseWindow = licenseWindowOpener;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsoleProvider = outputConsoleProvider;
        }
    }
}