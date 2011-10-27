using System.Diagnostics;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    public sealed class ProviderServices
    {
        public IUserNotifierServices UserNotifierServices { get; private set; }
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IScriptExecutor ScriptExecutor { get; private set; }
        public IOutputConsoleProvider OutputConsoleProvider { get; set; }
        public ISelectedProviderSettings SelectedProviderSettings { get; private set; }

        public ProviderServices() :
            this(new UserNotifierServices(),
                 new ProgressWindowOpener(),
                 new SelectedProviderSettingsManager(),
                 ServiceLocator.GetInstance<IScriptExecutor>(),
                 ServiceLocator.GetInstance<IOutputConsoleProvider>()) 
        {
        }

        public ProviderServices(
            IUserNotifierServices userNotifierServices,
            IProgressWindowOpener progressWindow,
            ISelectedProviderSettings selectedProviderSettings,
            IScriptExecutor scriptExecutor,
            IOutputConsoleProvider outputConsoleProvider)
        {
            UserNotifierServices = userNotifierServices;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsoleProvider = outputConsoleProvider;
            SelectedProviderSettings = selectedProviderSettings;
        }
    }
}