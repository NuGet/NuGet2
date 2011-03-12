using System;
using System.Management.Automation.Runspaces;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal interface IRunspaceManager {
        Tuple<Runspace, MyHost> GetRunspace(IConsole console, string hostName);
    }
}