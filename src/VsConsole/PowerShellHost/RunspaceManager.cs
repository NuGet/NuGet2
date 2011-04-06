using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Microsoft.PowerShell;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal class RunspaceManager : IRunspaceManager {
        private const string PSModulePathEnvVariable = "PSModulePath";

        // Cache Runspace by name. There should be only one Runspace instance created though.
        private readonly ConcurrentDictionary<string, Tuple<Runspace, NuGetPSHost>> _runspaceCache = new ConcurrentDictionary<string, Tuple<Runspace, NuGetPSHost>>();

        public const string ProfilePrefix = "NuGet";
        public const string NuGetCoreModuleName = "NuGet";

        public Tuple<Runspace, NuGetPSHost> GetRunspace(IConsole console, string hostName) {
            return _runspaceCache.GetOrAdd(hostName, name => CreateAndSetupRunspace(console, name));
        }

        private static Tuple<Runspace, NuGetPSHost> CreateAndSetupRunspace(IConsole console, string hostName) {
            // set up powershell environment variable for module search path
            // ensuring our own Modules folder is searched before system or user-level 
            AddPowerShellModuleSearchPath();

            Tuple<Runspace, NuGetPSHost> runspace = CreateRunspace(console, hostName);
            LoadStartupScripts(runspace.Item1);
            LoadProfilesIntoRunspace(runspace.Item1);

            return runspace;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification="We can't dispose it if we want to return it.")]
        private static Tuple<Runspace, NuGetPSHost> CreateRunspace(IConsole console, string hostName) {
            DTE dte = ServiceLocator.GetInstance<DTE>();

            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Variables.Add(
                new SessionStateVariableEntry(
                    "DTE", 
                    (DTE2)dte, 
                    "Visual Studio DTE automation object",
                    ScopedItemOptions.AllScope | ScopedItemOptions.Constant)
            );

            // this is used by the functional tests
            var packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            var privateData = Tuple.Create<string, object>("packageManagerFactory", packageManagerFactory);
            var host = new NuGetPSHost(hostName, privateData) {
                ActiveConsole = console
            };

            var runspace = RunspaceFactory.CreateRunspace(host, initialSessionState);
            runspace.ThreadOptions = PSThreadOptions.Default;
            runspace.Open();

            //
            // Set this runspace as DefaultRunspace so I can script DTE events.
            //
            // WARNING: MSDN says this is unsafe. The runspace must not be shared across
            // threads. I need this to be able to use ScriptBlock for DTE events. The
            // ScriptBlock event handlers execute on DefaultRunspace.
            //
            Runspace.DefaultRunspace = runspace;

            return Tuple.Create(runspace, host);
        }

        private static void AddPowerShellModuleSearchPath() {
            string psModulePath = Environment.GetEnvironmentVariable(PSModulePathEnvVariable) ?? String.Empty;
            string extensionRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // EnvironmentPermission demand?
            Environment.SetEnvironmentVariable(PSModulePathEnvVariable,
                                               String.Format(CultureInfo.InvariantCulture, "{0}\\Modules\\;{1}", extensionRoot, psModulePath),
                                               EnvironmentVariableTarget.Process);
        }

        private static void LoadStartupScripts(Runspace runspace) {
            ExecutionPolicy policy = runspace.GetEffectiveExecutionPolicy();
            if (policy != ExecutionPolicy.Unrestricted &&
                policy != ExecutionPolicy.RemoteSigned &&
                policy != ExecutionPolicy.Bypass) {

                ExecutionPolicy machinePolicy = runspace.GetExecutionPolicy(ExecutionPolicyScope.MachinePolicy);
                if (machinePolicy != ExecutionPolicy.Undefined) {
                    throw new InvalidOperationException(VsResources.Console_GroupPolicyError);
                }

                ExecutionPolicy userPolicy = runspace.GetExecutionPolicy(ExecutionPolicyScope.UserPolicy);
                if (userPolicy != ExecutionPolicy.Undefined) {
                    throw new InvalidOperationException(VsResources.Console_GroupPolicyError);
                }

                runspace.SetExecutionPolicy(ExecutionPolicy.RemoteSigned, ExecutionPolicyScope.Process);
            }

            runspace.ImportModule(NuGetCoreModuleName);

#if DEBUG
            if (File.Exists(DebugConstants.TestModulePath)) {
                runspace.ImportModule(DebugConstants.TestModulePath);
            }
#endif
        }

        private static void LoadProfilesIntoRunspace(Runspace runspace) {
            using (var powerShell = System.Management.Automation.PowerShell.Create()) {
                powerShell.Runspace = runspace;

                PSCommand[] profileCommands = HostUtilities.GetProfileCommands(ProfilePrefix);
                foreach (PSCommand command in profileCommands) {
                    powerShell.Commands = command;
                    powerShell.AddCommand("out-default");
                    powerShell.Invoke();
                }
            }
        }
    }
}