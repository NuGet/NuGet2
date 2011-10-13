
namespace NuGet.PowerShell.Commands
{
    /// <summary>
    /// This class houses locale-agnostic error identifiers which aid
    /// users searching for solutions to problems that may be in a different
    /// language than the human-readable error message accompanying them.
    /// </summary>
    internal static class NuGetErrorId
    {
        public const string NoCompatibleProjects = "NuGetNoCompatibleProjects";
        public const string ProjectNotFound = "NuGetProjectNotFound";
        public const string NoActiveSolution = "NuGetNoActiveSolution";
        public const string FileNotFound = "NuGetFileNotFound";
        public const string FileExistsNoClobber = "NuGetFileExistsNoClobber";
        public const string TooManySpecFiles = "NuGetTooManySpecFiles";
        public const string NuspecFileNotFound = "NuGetNuspecFileNotFound";
        public const string CmdletUnhandledException = "NuGetCmdletUnhandledException";
    }
}
