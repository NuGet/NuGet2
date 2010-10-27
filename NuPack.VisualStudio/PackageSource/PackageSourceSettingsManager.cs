using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace NuGet.VisualStudio {
    internal class PackageSourceSettingsManager {

        private const string SettingsRoot = "NuGet";
        private const string PackageSourcesSettingProperty = "PackageSources";
        private const string ActivePackageSourceSettingProperty = "ActivePackageSource";
        private const string IsFirstTimeSettingsProperty = "FirstTime";

        private WritableSettingsStore _userSettingsStore;
        private IServiceProvider _serviceProvider;

        public PackageSourceSettingsManager(IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider");
            }

            _serviceProvider = serviceProvider;
        }

        public bool IsFirstRunning {
            get {
                return UserSettingsStore.GetBoolean(SettingsRoot, IsFirstTimeSettingsProperty, true);
            }
            set {
                UserSettingsStore.SetBoolean(SettingsRoot, IsFirstTimeSettingsProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the string which encodes all PackageSources in the VS setting store.
        /// </summary>
        /// <value>The package sources string.</value>
        public string PackageSourcesString {
            get {
                return UserSettingsStore.GetString(SettingsRoot, PackageSourcesSettingProperty, "");
            }
            set {
                UserSettingsStore.SetString(SettingsRoot, PackageSourcesSettingProperty, value ?? "");
            }
        }

        /// <summary>
        /// Gets or sets the string which encodes the active PackageSource in the VS setting store
        /// </summary>
        /// <value>The active package source string.</value>
        public string ActivePackageSourceString {
            get {
                return UserSettingsStore.GetString(SettingsRoot, ActivePackageSourceSettingProperty, "");
            }
            set {
                _userSettingsStore.SetString(SettingsRoot, ActivePackageSourceSettingProperty, value ?? "");
            }
        }

        private WritableSettingsStore UserSettingsStore {
            get {
                if (_userSettingsStore == null) {
                    SettingsManager settingsManager = new ShellSettingsManager(_serviceProvider);
                    _userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                    // Ensure that the package collection exists before any callers attempt to use it.
                    if (!_userSettingsStore.CollectionExists(SettingsRoot)) {
                        _userSettingsStore.CreateCollection(SettingsRoot);
                    }
                }
                return _userSettingsStore;
            }
        }
    }
}
