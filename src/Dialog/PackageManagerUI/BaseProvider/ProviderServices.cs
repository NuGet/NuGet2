using System.ComponentModel.Composition;
using NuGet.Dialog.PackageManagerUI;
using NuGet.OutputWindowConsole;

namespace NuGet.Dialog.Providers {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public sealed class ProviderServices {

        // HACK: Can't import ILicenseWindowOpener into ProviderServices via MEF because it creates a cyclic dependency
        // TODO: Spin off ILicenseWindowOpener into a separate class
        public ILicenseWindowOpener LicenseWindow { get; internal set; }

        [Import]
        public IProgressWindowOpener ProgressWindow { get; set; }

        [Import]
        public IScriptExecutor ScriptExecutor { get; set; }

        [Import]
        public IOutputConsoleProvider OutputConsoleProvider { get; set; }

        public ProviderServices() {
        }

        // for unit tests
        internal ProviderServices(
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