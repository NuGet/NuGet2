using System;
using System.ComponentModel.Composition;
using System.Management.Automation.Runspaces;
using EnvDTE80;

namespace NuPackConsole.Host.PowerShell.Implementation
{
    [Export(typeof(IPowerShellHostService))]
    class PowerShellHostService : IPowerShellHostService
    {
        public IHost CreateHost(IConsole console, DTE2 dte, string name, bool isAsync, Action<InitialSessionState> init, object privateData)
        {
            return isAsync ?
                new AsyncPowerShellHost(console, dte, name, init, privateData) as IHost :
                new SyncPowerShellHost(console, dte, name, init, privateData) as IHost;
        }
    }
}
