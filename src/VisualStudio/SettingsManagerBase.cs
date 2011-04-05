using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace NuGet.VisualStudio {
    public abstract class SettingsManagerBase {

        private Lazy<SettingsManager> _settingsManager;

        protected SettingsManagerBase(IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider");
            }

            _settingsManager = new Lazy<SettingsManager>(() => new ShellSettingsManager(serviceProvider));
        }

        protected int ReadInt32(string settingsRoot, string property, int defaultValue = 0) {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            if (userSettingsStore.CollectionExists(settingsRoot)) {
                return userSettingsStore.GetInt32(settingsRoot, property, defaultValue);
            }
            else {
                return defaultValue;
            }
        }

        protected void WriteInt32(string settingsRoot, string property, int value) {
            WritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            userSettingsStore.SetInt32(settingsRoot, property, value);
        }

        protected string ReadString(string settingsRoot, string property, string defaultValue = "") {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            if (userSettingsStore.CollectionExists(settingsRoot)) {
                return userSettingsStore.GetString(settingsRoot, property, defaultValue);
            }
            else {
                return defaultValue;
            }
        }

        protected string[] ReadStrings(string settingsRoot, string[] properties, string defaultValue = "") {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            if (userSettingsStore.CollectionExists(settingsRoot)) {
                string[] values = new string[properties.Length];
                for (int i = 0; i < values.Length; i++) {
                    values[i] = userSettingsStore.GetString(settingsRoot, properties[i], defaultValue);
                }
                return values;
            }
            else {
                return null;
            }
        }

        protected bool DeleteProperty(string settingsRoot, string property) {
            WritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            return userSettingsStore.DeleteProperty(settingsRoot, property);
        }

        protected void WriteStrings(string settingsRoot, string[] properties, string[] values) {
            Debug.Assert(properties.Length == values.Length);

            WritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            for (int i = 0; i < properties.Length; i++) {
                userSettingsStore.SetString(settingsRoot, properties[i], values[i]);
            }
        }

        protected void WriteString(string settingsRoot, string property, string value) {
            WritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);

            userSettingsStore.SetString(settingsRoot, property, value);
        }

        protected void ClearAllSettings(string settingsRoot) {
            WritableSettingsStore userSettingsStore = _settingsManager.Value.GetWritableSettingsStore(SettingsScope.UserSettings);
            userSettingsStore.DeleteCollection(settingsRoot);
        }

        private WritableSettingsStore GetWritableSettingsStore(string settingsRoot) {
            WritableSettingsStore userSettingsStore = _settingsManager.Value.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!userSettingsStore.CollectionExists(settingsRoot)) {
                userSettingsStore.CreateCollection(settingsRoot);
            }
            return userSettingsStore;
        }
    }
}