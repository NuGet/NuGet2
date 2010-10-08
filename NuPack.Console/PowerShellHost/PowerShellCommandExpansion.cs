using System.ComponentModel.Composition;

namespace NuPackConsole.Host.PowerShell.Implementation {
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Performance",
        "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "MEF requires this class to be non-static.")]
    [
        Export(typeof(ICommandExpansionProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    class PowerShellCommandExpansionProvider : CommandExpansionProvider {
        // Empty
    }
}
