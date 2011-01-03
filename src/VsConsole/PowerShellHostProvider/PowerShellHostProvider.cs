using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using NuGetConsole.Host.PowerShell;
using NuGetConsole.Host.PowerShell.Implementation;

namespace NuGetConsole.Host.PowerShellProvider {
    [Export(typeof(IHostProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    [DisplayName("NuGet Provider")]
    internal class PowerShellHostProvider : IHostProvider {
        /// <summary>
        /// PowerConsole host name of PowerShell host.
        /// </summary>
        /// <remarks>
        /// Note: PowerConsole\Impl\PowerConsole\Settings.cs copies this name as default host. Keep in sync.
        /// </remarks>
        public const string HostName = "NuGetConsole.Host.PowerShell";

        /// <summary>
        /// This PowerShell host name. Used for PowerShell "$host".
        /// </summary>
        public const string PowerConsoleHostName = "Package Manager Host";
 
        public IHost CreateHost(IConsole console, bool async) {
            bool isPowerShell2Installed = RegistryHelper.CheckIfPowerShell2Installed();
            if (isPowerShell2Installed) {
                return CreatePowerShellHost(console, async);
            }
            else {
                return new UnsupportedHost(console);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IHost CreatePowerShellHost(IConsole console, bool async) {

            IHost host = PowerShellHostService.CreateHost(console, PowerConsoleHostName, async, new Commander(console));

            return host;
        }

        class Commander {
            private readonly IConsole _console;

            public Commander(IConsole console) {
                _console = console;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Microsoft.Performance", 
                "CA1811:AvoidUncalledPrivateCode",
                Justification="This method can be dynamically invoked from PS script.")]
            public void ClearHost() {
                _console.Clear();
            }
        }
    }
}
