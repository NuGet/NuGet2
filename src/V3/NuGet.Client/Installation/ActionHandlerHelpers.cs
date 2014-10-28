using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;

namespace NuGet.Client.Installation
{
    // Common code used by action handlers
    public static class ActionHandlerHelpers
    {
        public static void ExecutePowerShellScriptIfPresent(string scriptName, InstallationTarget target, IPackage package, string installPath, IExecutionLogger logger)
        {
            // If we don't have a project, we're at solution level
            //  The <Solution> string is only for tracing so it probably doesn't need to be loc'ed
            string projectName = target.Name;
            var targetFramework = target.GetSupportedFrameworks().FirstOrDefault();

            // Get the install script
            var scriptFile = FindScript(
                package, 
                scriptName,
                targetFramework);

            // If there is a script to run
            if (scriptFile != null)
            {
                // Get the powershell execution feature
                var powershell = target.TryGetFeature<PowerShellScriptExecutor>();
                if (powershell != null)
                {
                    NuGetTraceSources.ActionExecutor.Info(
                        "executingps1",
                        "[{0}] Running {2} for {1}",
                        projectName,
                        package.GetFullName(),
                        scriptFile.Path);
                    powershell.ExecuteScript(installPath, scriptFile.Path, package, target, logger);
                }
                else
                {
                    NuGetTraceSources.ActionExecutor.Warning(
                        "missing_powershell_feature",
                        "[{0}] Unable to run PowerShell script {2} for {1} because install target does not support PowerShell scripts.",
                        projectName,
                        package.GetFullName(),
                        scriptFile.Path);
                }
            }
            else
            {
                NuGetTraceSources.ActionExecutor.Info(
                    "nops1",
                    "[{0}] No {2} script for {1}.",
                    projectName,
                    package.GetFullName(),
                    scriptName);
            }
        }

        // Uses logic originally found in NuGet.VisualStudio.PackageExtensions.FindCompatibleToolFiles
        private static IPackageFile FindScript(IPackage package, string scriptName, FrameworkName targetFramework)
        {
            IEnumerable<IPackageFile> toolFiles;
            if (targetFramework == null)
            {
                return package
                    .GetToolFiles()
                    .FirstOrDefault(a => a.Path.Equals("tools\\" + scriptName, StringComparison.OrdinalIgnoreCase));
            }
            else if (VersionUtility.TryGetCompatibleItems(targetFramework, package.GetToolFiles(), out toolFiles))
            {
                return toolFiles.FirstOrDefault(p => p.EffectivePath.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
            }
            return null; // Nothing found
        }
    }
}
