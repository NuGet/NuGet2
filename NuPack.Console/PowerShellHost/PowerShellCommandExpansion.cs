using System.ComponentModel.Composition;

namespace NuPackConsole.Host.PowerShell.Implementation
{
    [Export(typeof(ICommandExpansionProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    class PowerShellCommandExpansionProvider : CommandExpansionProvider
    {
        // Empty
    }
}
