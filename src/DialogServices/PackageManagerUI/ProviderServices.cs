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
        public IProviderSettings ProviderSettings { get; private set; }
        public IVsCommonOperations VsCommonOperations { get; private set; }
        public IUpdateAllUIService UpdateAllUIService { get; private set; }

        public ProviderServices() :
            this(new UserNotifierServices(),
                 new ProgressWindowOpener(),
                 new ProviderSettingsManager(),
                 new UpdateAllUIService(),
                 ServiceLocator.GetInstance<IScriptExecutor>(),
                 ServiceLocator.GetInstance<IOutputConsoleProvider>(),
                 ServiceLocator.GetInstance<IVsCommonOperations>()) 
        {
        }

        public ProviderServices(
            IUserNotifierServices userNotifierServices,
            IProgressWindowOpener progressWindow,
            IProviderSettings selectedProviderSettings,
            IUpdateAllUIService updateAllUIService,
            IScriptExecutor scriptExecutor,
            IOutputConsoleProvider outputConsoleProvider,
            IVsCommonOperations vsCommonOperations)
        {
            UserNotifierServices = userNotifierServices;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsoleProvider = outputConsoleProvider;
            ProviderSettings = selectedProviderSettings;
            VsCommonOperations = vsCommonOperations;
            UpdateAllUIService = updateAllUIService;
        }
    }
}
