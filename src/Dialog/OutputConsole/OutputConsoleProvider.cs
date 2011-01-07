using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using NuGetConsole;

namespace NuGet.OutputWindowConsole {

    [Export(typeof(IOutputConsoleProvider))]
    public class OutputConsoleProvider : IOutputConsoleProvider {

        // MEF container within VS
        [Import]
        public IComponentModel ComponentModel {
            get;
            set;
        }

        [Import]
        public IServiceProvider ServiceProvider {
            get;
            set;
        }

        public IConsole CreateOutputConsole() {
            var outputWindow = (IVsOutputWindow)ServiceProvider.GetService(typeof(SVsOutputWindow));
            Debug.Assert(outputWindow != null);

            var vsUIShell = (IVsUIShell)ServiceProvider.GetService(typeof(SVsUIShell));
            Debug.Assert(vsUIShell != null);

            var console = new OutputConsole(outputWindow, vsUIShell);
            var hostProvider = GetPowerShellHostProvider();
            console.Host = hostProvider.CreateHost(console, @async: false);

            return console;
        }

        private IHostProvider GetPowerShellHostProvider() {
            // The PowerConsole design enables multiple hosts (PowerShell, Python, Ruby)
            // For the Output window console, we're only interested in the PowerShell host. 
            // Here we filter out the the PowerShell host provider based on its name.

            // The PowerShell host provider name is defined in PowerShellHostProvider.cs
            const string PowerShellHostProviderName = "NuGetConsole.Host.PowerShell";

            var exportProvider = ComponentModel.DefaultExportProvider;
            var hostProviderExports = exportProvider.GetExports<IHostProvider, IHostMetadata>();
            var psProvider = hostProviderExports.Where(export => export.Metadata.HostName == PowerShellHostProviderName).Single();

            return psProvider.Value;
        }
    }
}
