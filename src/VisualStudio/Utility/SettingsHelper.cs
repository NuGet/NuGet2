using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// This class is used by functional tests to change NuGet user settings.
    /// </summary>
    public static class SettingsHelper
    {
        /// <summary>
        /// Sets a NuGet user settings property.
        /// </summary>
        /// <param name="property">The name of the settings property to set.</param>
        /// <param name="value">The value of the settings property.</param>
        public static void Set(string property, string value)
        {
            var packageRestoreConsent = new PackageRestoreConsent(ServiceLocator.GetInstance<ISettings>());
            if (string.Equals(property, "PackageRestoreConsentGranted", StringComparison.OrdinalIgnoreCase))
            {
                packageRestoreConsent.IsGrantedInSettings = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            else if (string.Equals(property, "PackageRestoreIsAutomatic", StringComparison.OrdinalIgnoreCase))
            {
                packageRestoreConsent.IsAutomatic = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                string message = String.Format(
                    CultureInfo.CurrentCulture,
                    VsResources.InvalidSettingsProperty,
                    property);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Gets the VsSettings singleton object.
        /// </summary>
        /// <returns>The VsSettings object in the system.</returns>
        public static ISettings GetVsSettings()
        {
            return ServiceLocator.GetInstance<ISettings>();
        }
    }
}
