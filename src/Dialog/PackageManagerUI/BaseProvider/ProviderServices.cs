using NuGet.Dialog.PackageManagerUI;
using NuGetConsole;

namespace NuGet.Dialog.Providers {
    public sealed class ProviderServices {
        public ILicenseWindowOpener LicenseWindow { get; private set; }
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IScriptExecutor ScriptExecutor { get; private set; }
        public IConsole OutputConsole { get; private set; }

        public ProviderServices(
            ILicenseWindowOpener licenseWindow,
            IProgressWindowOpener progressWindow,
            IScriptExecutor scriptExecutor,
            IConsole outputConsole) {

            LicenseWindow = licenseWindow;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsole = outputConsole;
        }
    }
}
