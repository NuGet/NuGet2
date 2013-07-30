using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Common
{
    public static class ProjectHelper
    {
        private static readonly HashSet<string> _supportedProjectExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {  
            ".csproj",
            ".vbproj",
            ".fsproj",
        };

        public static HashSet<string> SupportedProjectExtensions {
            get
            {
                return _supportedProjectExtensions;
            }
        }

        public static bool TryGetProjectFile(out string projectFile)
        {
            return TryGetProjectFile(Directory.GetCurrentDirectory(), out projectFile);
        }

        public static bool TryGetProjectFile(string directory, out string projectFile)
        {
            projectFile = null;
            var candidates = GetProjectFiles(directory).ToList();

            switch (candidates.Count)
            {
                case 1:
                    projectFile = candidates.Single();
                    break;
            }

            return !String.IsNullOrEmpty(projectFile);
        }

        public static IEnumerable<string> GetProjectFiles(string directory)
        {
            var files = Directory.GetFiles(directory);

            return files.Where(file => _supportedProjectExtensions.Contains(Path.GetExtension(file) ?? string.Empty));
        }


        public static string GetSolutionDir(string projectDirectory)
        {
            string path = projectDirectory;

            // Only look 4 folders up to find the solution directory
            const int maxDepth = 5;
            int depth = 0;
            do
            {
                if (SolutionFileExists(path))
                {
                    return path;
                }

                path = Path.GetDirectoryName(path);
                
                depth++;
                //When you get to c:\, the parent path is null.
            } while (depth < maxDepth && path != null);

            return null;
        }

        private static bool SolutionFileExists(string path)
        {
            return Directory.GetFiles(path, "*.sln").Any();
        }
    }
}
