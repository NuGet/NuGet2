using EnvDTE80;

namespace NuGetConsole.Host.PowerShell.Implementation {

    public static class PowerShellHostService {

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification="Can't dispose an object if we want to return it.")]
        public static IHost CreateHost(IConsole console, DTE2 dte, string name, bool isAsync, object privateData) {
            return isAsync ?
                new AsyncPowerShellHost(console, dte, name, privateData) as IHost :
                new SyncPowerShellHost(console, dte, name, privateData) as IHost;
        }
    }
}
