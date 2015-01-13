//using System.IO;
//using System.Linq;
//using System.Runtime.Versioning;
//using EnvDTE;
//using NuGet.Client.Installation;
//using NuGet.VisualStudio;

//namespace NuGet.Client.VisualStudio
//{
//    public class VsPowerShellScriptExecutor : PowerShellScriptExecutor
//    {
//        // V3TODO: Move script execution logic entirely into this class or revamp IScriptExecutor to be more general purpose.

//        private readonly PSScriptExecutor _scriptExecutor;

//        public VsPowerShellScriptExecutor(IScriptExecutor scriptExecutor)
//        {
//            // We only work in VS where we know it's a PSScriptExecutor.
//            _scriptExecutor = (PSScriptExecutor)scriptExecutor;
//        }

//        public override void ExecuteScript(string packageInstallPath, string scriptRelativePath, object package, InstallationTarget target, IExecutionLogger logger)
//        {
//            IPackage packageObject = (IPackage)package;

//            // If we don't have a project, we're at solution level
//            string projectName = target.Name;
//            FrameworkName targetFramework = target.GetSupportedFrameworks().FirstOrDefault();

//            VsProject targetProject = target as VsProject;
//            Project dteProject = targetProject == null ? null : targetProject.DteProject;

//            string fullPath = Path.Combine(packageInstallPath, scriptRelativePath);
//            if (!File.Exists(fullPath))
//            {
//                VsNuGetTraceSources.VsPowerShellScriptExecutionFeature.Error(
//                    "missing_script",
//                    "[{0}] Unable to locate expected script file: {1}",
//                    projectName,
//                    fullPath);
//            }
//            else
//            {
//                VsNuGetTraceSources.VsPowerShellScriptExecutionFeature.Info(
//                    "executing",
//                    "[{0}] Executing script file: {1}",
//                    projectName,
//                    fullPath);

//                _scriptExecutor.ExecuteResolvedScript(
//                    fullPath,
//                    packageInstallPath,
//                    packageObject,
//                    dteProject,
//                    targetFramework,
//                    new ShimLogger(logger));
//            }
//        }
//    }
//}
