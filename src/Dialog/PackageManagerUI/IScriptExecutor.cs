using EnvDTE;

namespace NuGet.Dialog.PackageManagerUI {
    public interface IScriptExecutor {
        bool Execute(string installPath, string scriptFileName, IPackage package, Project project, ILogger logger);
    }
}
