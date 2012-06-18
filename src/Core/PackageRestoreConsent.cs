using System;
using System.Globalization;

namespace NuGet
{
    public class PackageRestoreConsent
    {
        private const string EnvironmentVariableName = "EnableNuGetPackageRestore";
        private const string PackageRestoreSection = "packageRestore";
        private const string PackageRestoreConsentKey = "enabled";
        private readonly ISettings _settings;
        private readonly IEnvironmentVariableReader _environmentReader;

        public PackageRestoreConsent(ISettings settings)
            : this(settings, new EnvironmentVariableWrapper())
        {
        }

        public PackageRestoreConsent(ISettings settings, IEnvironmentVariableReader environmentReader)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (environmentReader == null)
            {
                throw new ArgumentNullException("environmentReader");
            }
            _settings = settings;
            _environmentReader = environmentReader;
        }

        public bool IsGranted
        {
            get
            {
                string envValue = _environmentReader.GetEnvironmentVariable(EnvironmentVariableName).SafeTrim();
                return IsGrantedInSettings || IsSet(envValue);
            }
        }

        public bool IsGrantedInSettings
        {
            get
            {
                string settingsValue = _settings.GetValue(PackageRestoreSection, PackageRestoreConsentKey).SafeTrim();
                return IsSet(settingsValue);
            }
            set
            {
                if (!value)
                {
                    _settings.DeleteSection(PackageRestoreSection);
                }
                else
                {
                    _settings.SetValue(PackageRestoreSection, PackageRestoreConsentKey, value.ToString());
                }
            }
        }

        private static bool IsSet(string value)
        {
            bool boolResult;
            int intResult;

            return !String.IsNullOrEmpty(value) &&
                   ((Boolean.TryParse(value, out boolResult) && boolResult) ||
                   (Int32.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out intResult) && (intResult == 1)));
        }
    }
}