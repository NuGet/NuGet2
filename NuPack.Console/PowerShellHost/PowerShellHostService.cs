using EnvDTE80;

namespace NuGetConsole.Host.PowerShell.Implementation {

    public static class PowerShellHostService {

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification="Can't dispose an object if we want to return it.")]
        public static IHost CreateHost(IConsole console, string name, bool isAsync, object privateData) {
            return isAsync ?
                new AsyncPowerShellHost(console, name, privateData) as IHost :
                new SyncPowerShellHost(console, name, privateData) as IHost;
        }
    }
}
