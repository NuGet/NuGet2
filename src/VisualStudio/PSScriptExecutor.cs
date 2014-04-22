using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using EnvDTE;
using NuGet.Resources;
using NuGet.VisualStudio.Resources;
using NuGetConsole;

namespace NuGet.VisualStudio
{
    [Export(typeof(IScriptExecutor))]
    public class PSScriptExecutor : IScriptExecutor
    {
        private readonly Lazy<IHost> _host;

        public PSScriptExecutor()
        {
            _host = new Lazy<IHost>(GetHost);
        }

        private IHost Host
        {
            get
            {
                return _host.Value;
            }
        }

        [Import]
        public IOutputConsoleProvider OutputConsoleProvider
        {
            get;
            set;
        }

        public bool Execute(string installPath, string scriptFileName, IPackage package, Project project, ILogger logger)
        {
            return Execute(installPath, scriptFileName, package, project, project.GetTargetFrameworkName(), logger);
        }

        public bool Execute(string installPath, string scriptFileName, IPackage package, Project project, FrameworkName targetFramework, ILogger logger)
        {
            string fullPath;
            IPackageFile scriptFile;
            if (package.FindCompatibleToolFiles(scriptFileName, targetFramework, out scriptFile))
            {
                fullPath = Path.Combine(installPath, scriptFile.Path);
            }
            else
            {
                return false;
            }

            if (File.Exists(fullPath))
            {
                if (project != null && scriptFile != null)
                {
                    // targetFramework can be null for unknown project types
                    string shortFramework = targetFramework == null ? string.Empty : VersionUtility.GetShortFrameworkName(targetFramework);

                    logger.Log(MessageLevel.Debug, NuGetResources.Debug_TargetFrameworkInfoPrefix, package.GetFullName(), 
                        project.Name, shortFramework);

                    logger.Log(MessageLevel.Debug, NuGetResources.Debug_TargetFrameworkInfo_PowershellScripts,
                        Path.GetDirectoryName(scriptFile.Path), VersionUtility.GetTargetFrameworkLogString(scriptFile.TargetFramework));
                }

                string toolsPath = Path.GetDirectoryName(fullPath);
                string logMessage = String.Format(CultureInfo.CurrentCulture, VsResources.ExecutingScript, fullPath);

                // logging to both the Output window and progress window.
                logger.Log(MessageLevel.Info, logMessage);

                IConsole console = OutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: true);
                Host.Execute(console,
                    "$__pc_args=@(); $input|%{$__pc_args+=$_}; & " + PathHelper.EscapePSPath(fullPath) + " $__pc_args[0] $__pc_args[1] $__pc_args[2] $__pc_args[3]; Remove-Variable __pc_args -Scope 0",
                    new object[] { installPath, toolsPath, package, project });

                return true;
            }
            return false;
        }

        private IHost GetHost()
        {
            // create the console and instantiate the PS host on demand
            IConsole console = OutputConsoleProvider.CreateOutputConsole(requirePowerShellHost: true);
            IHost host = console.Host;

            // start the console 
            console.Dispatcher.Start();

            // gives the host a chance to do initialization works before dispatching commands to it
            host.Initialize(console);

            // after the host initializes, it may set IsCommandEnabled = false
            if (host.IsCommandEnabled)
            {
                return host;
            }
            else
            {
                // the PowerShell host fails to initialize if group policy restricts to AllSigned
                throw new InvalidOperationException(VsResources.Console_InitializeHostFails);
            }
        }
    }
}