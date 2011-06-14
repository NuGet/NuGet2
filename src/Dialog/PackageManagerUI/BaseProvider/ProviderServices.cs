using System.ComponentModel.Composition;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public sealed class ProviderServices {
        public IUserNotifierServices WindowServices { get; private set; }
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IScriptExecutor ScriptExecutor { get; private set; }
        public IOutputConsoleProvider OutputConsoleProvider { get; private set; }

        [ImportingConstructor]
        public ProviderServices(
            IUserNotifierServices userNotifierServices,
            IProgressWindowOpener progressWindow,
            IScriptExecutor scriptExecutor,
            IOutputConsoleProvider outputConsoleProvider) {

            WindowServices = userNotifierServices;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsoleProvider = outputConsoleProvider;
        }
    }
}