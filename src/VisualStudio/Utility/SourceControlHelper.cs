using System;

namespace NuGet.VisualStudio
{
    internal static class SourceControlHelper
    {
        private const string SolutionSection = "solution";
        private const string DisableSourceControlIntegerationKey = "disableSourceControlIntegration";

        public static bool IsSourceControlDisabled(this ISettings settings)
        {
            var value = settings.GetValue(
                SolutionSection, 
                DisableSourceControlIntegerationKey,
                isPath: false);
            bool disableSourceControlIntegration;
            return !String.IsNullOrEmpty(value) && Boolean.TryParse(value, out disableSourceControlIntegration) && disableSourceControlIntegration;
        }

        public static void DisableSourceControlMode(this ISettings settings)
        {
            settings.SetValue(SolutionSection, DisableSourceControlIntegerationKey, "true");
        }
    }
}