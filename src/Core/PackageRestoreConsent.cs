using System;
using System.Globalization;

namespace NuGet
{
    public class PackageRestoreConsent
    {
        internal const string EnvironmentVariableName = "EnableNuGetPackageRestore";
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
                string value = _settings.GetValue(PackageRestoreSection, PackageRestoreConsentKey).SafeTrim();
                if (String.IsNullOrEmpty(value))
                {
                    value = _environmentReader.GetEnvironmentVariable(EnvironmentVariableName).SafeTrim();
                }

                if (!String.IsNullOrEmpty(value))
                {
                    bool boolResult;
                    int intResult;

                    return
                       (Boolean.TryParse(value, out boolResult) && boolResult) ||
                       (Int32.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out intResult) && (intResult == 1));
                }

                return false;
            }
            set
            {
                _settings.SetValue(PackageRestoreSection, PackageRestoreConsentKey, value.ToString());
            }
        }
    }
}