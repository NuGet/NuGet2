using System.ComponentModel.Composition;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public sealed class ProviderServices
    {
        public IUserNotifierServices WindowServices { get; private set; }
        public IProgressWindowOpener ProgressWindow { get; private set; }
        public IScriptExecutor ScriptExecutor { get; private set; }
        public IOutputConsoleProvider OutputConsoleProvider { get; set; }
        public ISelectedProviderSettings SelectedProviderSettings { get; private set; }
        public IVsCommonOperations VsCommonOperations { get; private set; }

        public ProviderServices() :
            this(new UserNotifierServices(),
                 new ProgressWindowOpener(),
                 new SelectedProviderSettingsManager(),
                 ServiceLocator.GetInstance<IScriptExecutor>(),
                 ServiceLocator.GetInstance<IOutputConsoleProvider>(),
                 ServiceLocator.GetInstance<IVsCommonOperations>()) 
        {
        }

        public ProviderServices(
            IUserNotifierServices userNotifierServices,
            IProgressWindowOpener progressWindow,
            IScriptExecutor scriptExecutor,
            IOutputConsoleProvider outputConsoleProvider,
            IVsCommonOperations vsCommonOperations)
        {

            WindowServices = userNotifierServices;
            ProgressWindow = progressWindow;
            ScriptExecutor = scriptExecutor;
            OutputConsoleProvider = outputConsoleProvider;
            SelectedProviderSettings = selectedProviderSettings;
            VsCommonOperations = vsCommonOperations;
        }
    }
}