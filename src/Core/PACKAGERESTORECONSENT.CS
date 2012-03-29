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
            : this(settings, EnvironmentVariableWrapper.Default)
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
                string value = _settings.GetValue(PackageRestoreSection, PackageRestoreConsentKey);
                if (value == null)
                {
                    // if the key is not set in nuget.config, read the environment variable
                    value =
                        _environmentReader.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.User) ??
                        _environmentReader.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.Machine);
                }

                return Boolean.TrueString.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                       "1".Equals(value, StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                if (value)
                {
                    _settings.SetValue(PackageRestoreSection, PackageRestoreConsentKey, "true");
                }
                else
                {
                    _settings.DeleteValue(PackageRestoreSection, PackageRestoreConsentKey);
                }
            }
        }
    }
}