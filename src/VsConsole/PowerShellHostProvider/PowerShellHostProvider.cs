using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
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
        /// </remarks>
        public const string HostName = "NuGetConsole.Host.PowerShell";

        /// <summary>
        /// This PowerShell host name. Used for PowerShell "$host".
        /// </summary>
        public const string PowerConsoleHostName = "Package Manager Host";

        private IHost _host;

        public IHost CreateHost(bool @async) {
            if (_host == null) {
                bool isPowerShell2Installed = RegistryHelper.CheckIfPowerShell2Installed();
                if (isPowerShell2Installed) {
                    _host = CreatePowerShellHost(@async);
                }
                else {
                    _host = new UnsupportedHost();
                }
            }

            return _host;

            // backdoor: allow turning off async mode by setting enviroment variable NuGetSyncMode=1
            string syncModeFlag = Environment.GetEnvironmentVariable("NuGetSyncMode", EnvironmentVariableTarget.User);
            if (syncModeFlag == "1") {
                @async = false;
            }

        [MethodImpl(MethodImplOptions.NoInlining)]
            return PowerShellHostService.CreateHost(PowerConsoleHostName, @async, new Commander(null));
        }

        class Commander {
            private readonly IConsole _console;

            public Commander(IConsole console) {
                _console = console;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Microsoft.Performance",
                "CA1811:AvoidUncalledPrivateCode",
                Justification = "This method can be dynamically invoked from PS script.")]
            public void ClearHost() {
                _console.Clear();
            }
        }
    }
}