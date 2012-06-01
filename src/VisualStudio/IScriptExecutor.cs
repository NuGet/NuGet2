using System;
using System.Runtime.Versioning;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IScriptExecutor
    {
        // Ideally, we should delete this method but it is called by the MvcScaffolding package.
        [Obsolete("Call the other overload which accepts the FrameworkName parameter.")]
        bool Execute(string installPath, string scriptFileName, IPackage package, Project project, ILogger logger);

        bool Execute(string installPath, string scriptFileName, IPackage package, Project project, FrameworkName targetFramework, ILogger logger);
    }
}