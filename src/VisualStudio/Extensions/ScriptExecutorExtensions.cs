using EnvDTE;
using System.Runtime.Versioning;

namespace NuGet.VisualStudio
{
    public static class ScriptExecutorExtensions
    {
        public static void ExecuteScript(
            this IScriptExecutor executor, 
            string installPath, 
            string scriptFileName, 
            IPackage package,
            Project project,
            FrameworkName targetFramework,
            ILogger logger)
        {
            if (package.HasPowerShellScript(new[] { scriptFileName }))
            {
                executor.Execute(
                    installPath, 
                    scriptFileName, 
                    package, 
                    project, 
                    targetFramework, 
                    logger);
            }
        }

        public static void ExecuteInitScript(this IScriptExecutor executor, string installPath, IPackage package, ILogger logger)
        {
            executor.ExecuteScript(installPath, PowerShellScripts.Init, package, project: null, targetFramework: null, logger: logger);
        }
    }
}
