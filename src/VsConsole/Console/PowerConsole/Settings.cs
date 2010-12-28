using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace NuGetConsole.Implementation.PowerConsole {
    /// <summary>
    /// Managed some user settings.
    /// </summary>
    internal static class Settings {
        /// <summary>
        /// The user settings collection path used by this package.
        /// </summary>
        const string CollectionPath = PowerConsoleWindow.ContentType;

        /// <summary>
        /// ActiveHost user settings property name.
        /// </summary>
        const string ActiveHostPropertyName = "ActiveHost";

        /// <summary>
        /// Default active host.
        /// </summary>
        const string DefActiveHost = "NuGetConsole.Host.PowerShell";

        /// <summary>
        /// Get default active host user settings.
        /// </summary>
        public static void GetDefaultHost(IServiceProvider sp, out string defHost) {
            defHost = null;

            IVsSettingsManager sm = sp.GetService<IVsSettingsManager>(typeof(SVsSettingsManager));
            if (sm != null) {
                IVsSettingsStore settingsStore;
                ErrorHandler.ThrowOnFailure(sm.GetReadOnlySettingsStore(
                    (uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore));

                int fExists;
                ErrorHandler.ThrowOnFailure(settingsStore.CollectionExists(CollectionPath, out fExists));
                if (fExists != 0) {
                    ErrorHandler.ThrowOnFailure(settingsStore.GetStringOrDefault(
                        CollectionPath, ActiveHostPropertyName, DefActiveHost, out defHost));
                }
            }

            if (defHost == null) {
                defHost = DefActiveHost;
            }
        }

        /// <summary>
        /// Set default active host in user settings.
        /// </summary>
        public static void SetDefaultHost(IServiceProvider sp, string defHost) {
            if (string.IsNullOrEmpty(defHost)) {
                return;
            }

            IVsSettingsManager sm = sp.GetService<IVsSettingsManager>(typeof(SVsSettingsManager));
            if (sm != null) {
                IVsWritableSettingsStore settingsStore;
                ErrorHandler.ThrowOnFailure(sm.GetWritableSettingsStore(
                    (uint)__VsSettingsScope.SettingsScope_UserSettings, out settingsStore));

                int fExists;
                ErrorHandler.ThrowOnFailure(settingsStore.CollectionExists(CollectionPath, out fExists));
                if (fExists == 0) {
                    ErrorHandler.ThrowOnFailure(settingsStore.CreateCollection(CollectionPath));
                }

                ErrorHandler.ThrowOnFailure(
                    settingsStore.SetString(CollectionPath, ActiveHostPropertyName, defHost));
            }
        }
    }
}
