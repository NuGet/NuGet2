using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using EnvDTE;
using NuGet.Client.Installation;
using NuGet.VisualStudio;

namespace NuGet.Client.VisualStudio
{
    internal class VsPowerShellScriptExecutionFeature : PowerShellScriptExecutionFeature
    {
        // V3TODO: Move script execution logic entirely into this class or revamp IScriptExecutor to be more general purpose.

        private readonly PSScriptExecutor _scriptExecutor;

        public VsPowerShellScriptExecutionFeature(IScriptExecutor scriptExecutor)
        {
            // We only work in VS where we know it's a PSScriptExecutor.
            _scriptExecutor = (PSScriptExecutor)scriptExecutor;
        }

        public override void ExecuteScript(string packageInstallPath, string scriptRelativePath, IPackage package, TargetProject project, IExecutionLogger logger)
        {
            // If we don't have a project, we're at solution level
            string projectName = project == null ? "<Solution>" : project.Name;
            FrameworkName targetFramework = project == null ? null : project.GetSupportedFramework();
            Project dteProject = project == null ? null : ((VsTargetProject)project).Project;

            string fullPath = Path.Combine(packageInstallPath, scriptRelativePath);
            if (!File.Exists(fullPath))
            {
                VsNuGetTraceSources.VsPowerShellScriptExecutionFeature.Error(
                    "missing_script",
                    "[{0}] Unable to locate expected script file: {1}",
                    projectName,
                    fullPath);
            }
            else
            {
                VsNuGetTraceSources.VsPowerShellScriptExecutionFeature.Info(
                    "executing",
                    "[{0}] Executing script file: {1}",
                    projectName,
                    fullPath);

                _scriptExecutor.ExecuteResolvedScript(
                    fullPath,
                    packageInstallPath,
                    package,
                    dteProject,
                    targetFramework,
                    new ShimLogger(logger));
            }
        }
    }
}
