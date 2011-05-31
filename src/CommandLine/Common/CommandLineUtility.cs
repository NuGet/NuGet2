using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Common;

namespace NuGet {
    internal static class CommandLineUtility {
        public readonly static string ApiKeysSectionName = "apikeys";
        private static readonly HashSet<string> _supportedProjectExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {  
            ".csproj",
            ".vbproj",
            ".fsproj",
        };

        public static string GetApiKey(ISettings settings, string source, bool throwIfNotFound = true) {
            var value = settings.GetDecryptedValue(CommandLineUtility.ApiKeysSectionName, source);
            if (String.IsNullOrEmpty(value) && throwIfNotFound) {
                throw new CommandLineException(NuGetResources.NoApiKeyFound, GetSourceDisplayName(source));
            }
            return value;
        }

        public static string GetSourceDisplayName(string source) {
            if (String.IsNullOrEmpty(source) || source.Equals(GalleryServer.DefaultGalleryServerUrl)) {
                return NuGetResources.LiveFeed + " (" + GalleryServer.DefaultGalleryServerUrl + ")";
            }
            if (source.Equals(GalleryServer.DefaultSymbolServerUrl)) {
                return NuGetResources.DefaultSymbolServer + " (" + GalleryServer.DefaultSymbolServerUrl + ")";
            }
            return "'" + source + "'";
        }

        public static void ValidateSource(string source) {
            if (!PathValidator.IsValidUrl(source)) {
                throw new CommandLineException(NuGetResources.InvalidSource, source);
            }
        }

        public static bool TryGetProjectFile(out string projectFile) {
            return TryGetProjectFile(Directory.GetCurrentDirectory(), out projectFile);
        }

        public static bool TryGetProjectFile(string directory, out string projectFile) {
            projectFile = null;
            var files = Directory.GetFiles(directory);

            var candidates = files.Where(file => _supportedProjectExtensions.Contains(Path.GetExtension(file)))
                                  .ToList();

            switch (candidates.Count) {
                case 1:
                    projectFile = candidates.Single();
                    break;
            }

            return !String.IsNullOrEmpty(projectFile);
        }

        public static string GetSolutionDir(string projectDirectory) {
            string path = projectDirectory;

            // Only look 4 folders up to find the solution directory
            const int maxDepth = 5;
            int depth = 0;
            do {
                if (SolutionFileExists(path)) {
                    return path;
                }

                path = Path.GetDirectoryName(path);

                depth++;
            } while (depth < maxDepth);

            return null;
        }

        public static string GetUnambiguousFile(string searchPattern) {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPattern);
            if (files.Length == 1) {
                return files[0];
            }

            return null;
        }

        private static bool SolutionFileExists(string path) {
            return Directory.GetFiles(path, "*.sln").Any();
        }

#if DEBUG
        internal static void WaitForDebugger() {
            System.Console.WriteLine("Waiting for debugger");
            while (!System.Diagnostics.Debugger.IsAttached) {

            }
        }
#endif
    }
}
