using System;
using System.Management.Automation.Runspaces;
using EnvDTE80;

namespace NuPackConsole.Host.PowerShell {
    public interface IPowerShellHostService {
        IHost CreateHost(IConsole console, DTE2 dte, string name = null, bool isAsync = false, object privateData = null);
    }
}
