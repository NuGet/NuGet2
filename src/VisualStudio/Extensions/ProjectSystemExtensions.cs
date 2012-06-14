using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.VisualStudio
{
    internal static class ProjectSystemExtensions
    {
        private const string RefreshFileExtension = ".refresh";

        /// <summary>
        /// Creates a .refresh file in bin directory of the IFileSystem that points to the assembly being installed. 
        /// This works around issues in DTE's AddReference method when dealing with GACed binaries.
        /// </summary>
        /// <param name="fileSystem">The project system the assembly is being referenced by.</param>
        /// <param name="assemblyPath">The relative path to the assembly being added</param>
        public static void CreateRefreshFile(this IFileSystem fileSystem, string assemblyPath)
        {
            string referenceName = Path.GetFileName(assemblyPath);
            string refreshFilePath = Path.Combine("bin", referenceName + RefreshFileExtension);
            if (!fileSystem.FileExists(refreshFilePath))
            {
                string projectPath = PathUtility.EnsureTrailingSlash(fileSystem.Root);
                string relativeAssemblyPath = PathUtility.GetRelativePath(projectPath, assemblyPath);

                try
                {
                    using (var stream = relativeAssemblyPath.AsStream())
                    {
                        fileSystem.AddFile(refreshFilePath, stream);
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    // log IO permission error
                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }

        public static IEnumerable<string> ResolveRefreshPaths(this IFileSystem fileSystem)
        {
            // Resolve all .refresh files from the website's bin directory. Once resolved, verify the path exists on disk and that they look like an assembly reference. 
            return from file in fileSystem.GetFiles("bin", "*" + RefreshFileExtension)
                   let resolvedPath = SafeResolveRefreshPath(fileSystem, file)
                   where resolvedPath != null && 
                         fileSystem.FileExists(resolvedPath) && 
                         Constants.AssemblyReferencesExtensions.Contains(Path.GetExtension(resolvedPath))
                   select resolvedPath;
        }

        private static string SafeResolveRefreshPath(IFileSystem fileSystem, string file)
        {
            string relativePath;
            try
            {
                using (var stream = fileSystem.OpenFile(file))
                {
                    relativePath = stream.ReadToEnd();
                }
                return fileSystem.GetFullPath(relativePath);
            }
            catch 
            {
                // Ignore the .refresh file if it cannot be read.
            }
            return null;
        }
    }
}
