using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace NuPack.Dialog.ToolsOptionsUI {
    internal static class Settings {
        public const string SettingsRoot = "NuPack";
        public const string RepositoryServiceUriSetting = "RepositoryServiceURI";
        public const string RepositoryServiceUriDefault = "";
        private static SettingsStore configurationSettingsStore;
        private static WritableSettingsStore userSettingsStore;

        private static WritableSettingsStore UserSettingsStore {
            get {
                if (userSettingsStore == null) {
                    SettingsManager settingsManager = new ShellSettingsManager(Utilities.ServiceProvider);
                    userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                    //Ensure that the package collection exists before any callers attempt to use it.
                    if (!userSettingsStore.CollectionExists(SettingsRoot)) {
                        userSettingsStore.CreateCollection(SettingsRoot);
                    }
                }
                return userSettingsStore;
            }
        }

        private static SettingsStore ConfigurationSettingsStore {
            get {
                if (configurationSettingsStore == null) {
                    SettingsManager settingsManager = new ShellSettingsManager(Utilities.ServiceProvider);
                    configurationSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.Configuration);
                }
                return configurationSettingsStore;
            }
        }

        public static string RepositoryServiceUri {
            get {
                return UserSettingsStore.GetString(SettingsRoot, RepositoryServiceUriSetting, RepositoryServiceUriDefault);
            }
            set {
                UserSettingsStore.SetString(SettingsRoot, RepositoryServiceUriSetting, value);
            }
        }
    }
}
