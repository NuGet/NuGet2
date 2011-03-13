using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using EnvDTE;
using NuGet.VisualStudio.Resources;
using NuGetConsole;

namespace NuGet.VisualStudio {
    [Export(typeof(IScriptExecutor))]
    public class PSScriptExecutor : IScriptExecutor {
        private readonly Lazy<IHost> _host;

        public PSScriptExecutor() {
            _host = new Lazy<IHost>(GetHost);
        }

        private IHost Host {
            get {
                return _host.Value;
            }
        }

        [Import]
        public IOutputConsoleProvider OutputConsoleProvider {
            get;
            set;
        }

        public bool Execute(string installPath, string scriptFileName, IPackage package, Project project, ILogger logger) {
            string toolsPath = Path.Combine(installPath, "tools");
            string fullPath = Path.Combine(toolsPath, scriptFileName);
            if (File.Exists(fullPath)) {
                string logMessage = String.Format(CultureInfo.CurrentCulture, VsResources.ExecutingScript, fullPath);

                // logging to both the Output window and progress window.
                logger.Log(MessageLevel.Info, logMessage);

                IConsole console = OutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: true);
                Host.Execute(console,
                    "$__pc_args=@(); $input|%{$__pc_args+=$_}; & '" + fullPath + "' $__pc_args[0] $__pc_args[1] $__pc_args[2] $__pc_args[3]; Remove-Variable __pc_args -Scope 0",
                    new object[] { installPath, toolsPath, package, project });

                return true;
            }
            return false;
        }

        private IHost GetHost() {
            // create the console and instantiate the PS host on demand
            IConsole console = OutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: true);
            IHost host = console.Host;

            // start the console 
            console.Dispatcher.Start();

            // gives the host a chance to do initialization works before dispatching commands to it
            host.Initialize(console);

            // after the host initializes, it may set IsCommandEnabled = false
            if (host.IsCommandEnabled) {
                return host;
            }
            else {
                // the PowerShell host fails to initialize if group policy restricts loading of scripts
                throw new InvalidOperationException(VsResources.Console_GroupPolicyError);
            }
        }
    }
}