using System;
using System.ComponentModel.Composition;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace NuPackConsole.Host.PowerShell.Implementation {
    [Export(typeof(IHostProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    [DisplayName("NuPack Provider")]
    class PowerShellHostProvider : IHostProvider {
        /// <summary>
        /// PowerConsole host name of PowerShell host.
        /// </summary>
        /// <remarks>
        /// Note: PowerConsole\Impl\PowerConsole\Settings.cs copies this name as default host. Keep in sync.
        /// </remarks>
        public const string HostName = "NuPackConsole.Host.PowerShell";

        /// <summary>
        /// This PowerShell host name. Used for PowerShell "$host".
        /// </summary>
        public const string PowerConsoleHostName = "NuPack";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification="MEF")]
        [Import]
        internal IPowerShellHostService PowerShellHostService { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "MEF")]
        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider { get; set; }

        public IHost CreateHost(IConsole console) {
            DTE2 dte = ServiceProvider.GetService<DTE2>(typeof(DTE));

            IHost host = PowerShellHostService.CreateHost(
                console,
                dte,
                PowerConsoleHostName,
                /*isAsync*/false,
                new Commander(console));

            console.Dispatcher.Starting += (sender, e) => {
                IPowerShellHost psHost = host as IPowerShellHost;
                if (psHost != null) {
                    psHost.Initialize();
                }
            };

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
