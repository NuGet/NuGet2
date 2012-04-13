namespace NuGet
{
    public sealed class NuGetConfigSettingsProvider : ISettingsProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification="This type is immutable.")]
        public static readonly NuGetConfigSettingsProvider Default = new NuGetConfigSettingsProvider();

        private NuGetConfigSettingsProvider()
        {
        }

        public ISettings LoadUserSettings()
        {
            return Settings.LoadDefaultSettings();
        }
    }
}