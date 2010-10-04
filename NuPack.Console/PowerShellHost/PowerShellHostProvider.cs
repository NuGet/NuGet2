using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace NuPackConsole.Host.PowerShell.Implementation
{
    [Export(typeof(IHostProvider))]
    [HostName(PowerShellHostProvider.HostName)]
    [DisplayName("NuPack Provider")]
    class PowerShellHostProvider : IHostProvider
    {
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

        [Import]
        internal IPowerShellHostService PowerShellHostService { get; set; }

        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider { get; set; }
      
        public IHost CreateHost(IConsole console)
        {
            DTE2 dte = ServiceProvider.GetService<DTE2>(typeof(DTE));

            IHost host = PowerShellHostService.CreateHost(
                console, 
                dte,
                PowerConsoleHostName, 
                /*isAsync*/false,
                new Commander(console));

			console.Dispatcher.BeforeStart += (sender, e) =>
			{
                IPowerShellHost psHost = host as IPowerShellHost;
                if (psHost != null)
                {
                    psHost.Initialize();
                }
			};

            return host;
        }

        class Commander
        {
            IConsole Console { get; set; }

            public Commander(IConsole console)
            {
                this.Console = console;
            }

            public void ClearHost()
            {
                Console.Clear();
            }
        }
    }
}
