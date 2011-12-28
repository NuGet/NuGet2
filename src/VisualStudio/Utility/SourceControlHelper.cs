using System;
using System.IO;

namespace NuGet.VisualStudio
{
    internal static class SourceControlHelper
    {
        private const string DotNuGetFolder = ".nuget";
        private const string SolutionSection = "solution";
        private const string DisableSourceControlIntegerationKey = "disableSourceControlIntegration";

        public static bool IsSourceControlDisabled(this ISettings settings)
        {
            var value = settings.GetValue(SolutionSection, DisableSourceControlIntegerationKey);
            bool disableSourceControlIntegration;
            return !String.IsNullOrEmpty(value) && Boolean.TryParse(value, out disableSourceControlIntegration) && disableSourceControlIntegration;
        }

        public static bool IsSourceControlDisabled(this IFileSystemProvider fileSystemProvider, string solutionDirectory)
        {
            var nugetFolder = Path.Combine(solutionDirectory, DotNuGetFolder);
            IFileSystem fileSystem = fileSystemProvider.GetFileSystem(nugetFolder);
            if (fileSystem == null)
            {
                return false;
            }
            var settings = new Settings(fileSystem);
            return IsSourceControlDisabled(settings);
        }

        public static void DisableSourceControlMode(this ISettings settings)
        {
            settings.SetValue(SolutionSection, DisableSourceControlIntegerationKey, "true");
        }
    }
}