using System;
using System.IO;

namespace NuGet.VisualStudio
{
    public static class ProjectSystemExtensions
    {
        /// <summary>
        /// Creates a .refresh file in bin directory of the IProjectSystem that points to the assembly being installed. 
        /// This works around issues in DTE's AddReference method when dealing with GACed binaries.
        /// </summary>
        /// <param name="projectSystem">The project system the assembly is being referenced by.</param>
        /// <param name="assemblyPath">The relative path to the assembly being added</param>
        internal static void CreateRefreshFile(this IProjectSystem projectSystem, string assemblyPath)
        {
            string referenceName = Path.GetFileName(assemblyPath);
            string refreshFilePath = Path.Combine("bin", referenceName + ".refresh");
            if (!projectSystem.FileExists(refreshFilePath))
            {
                string projectPath = PathUtility.EnsureTrailingSlash(projectSystem.Root);
                string relativeAssemblyPath = PathUtility.GetRelativePath(projectPath, assemblyPath);

                try
                {
                    using (var stream = relativeAssemblyPath.AsStream())
                    {
                        projectSystem.AddFile(refreshFilePath, stream);
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    // log IO permission error
                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }
    }
}
