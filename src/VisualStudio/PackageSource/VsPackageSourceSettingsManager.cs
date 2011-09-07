using System;
using System.ComponentModel.Composition;

namespace NuGet.VisualStudio {
    [Export(typeof(IPackageSourceSettingsManager))]
    public class VsPackageSourceSettingsManager : SettingsManagerBase, IPackageSourceSettingsManager {
        private const string SettingsRoot = "NuGet";
        private const string PackageSourcesSettingProperty = "PackageSources";
        private const string ActivePackageSourceSettingProperty = "ActivePackageSource";

        public VsPackageSourceSettingsManager()
            : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsPackageSourceSettingsManager(IServiceProvider serviceProvider)
            : base(serviceProvider) {
        }

        /// <summary>
        /// Gets or sets the string which encodes all PackageSources in the VS setting store.
        /// </summary>
        /// <value>The package sources string.</value>
        public string PackageSourcesString {
            get {
                return ReadString(SettingsRoot, PackageSourcesSettingProperty, "");
            }
            set {
                if (value == null) {
                    DeleteProperty(SettingsRoot, PackageSourcesSettingProperty);
                }
                else {
                    WriteString(SettingsRoot, PackageSourcesSettingProperty, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the string which encodes the active PackageSource in the VS setting store
        /// </summary>
        /// <value>The active package source string.</value>
        public string ActivePackageSourceString {
            get {
                return ReadString(SettingsRoot, ActivePackageSourceSettingProperty, "");
            }
            set {
                if (value == null) {
                    DeleteProperty(SettingsRoot, ActivePackageSourceSettingProperty);
                }
                else {
                    WriteString(SettingsRoot, ActivePackageSourceSettingProperty, value);
                }
            }
        }
    }
}