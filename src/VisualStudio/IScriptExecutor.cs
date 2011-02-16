using EnvDTE;

namespace NuGet.VisualStudio {
    public interface IScriptExecutor {
        bool Execute(string installPath, string scriptFileName, IPackage package, Project project, ILogger logger);
    }
}
