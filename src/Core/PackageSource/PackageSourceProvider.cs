using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    public class PackageSourceProvider : IPackageSourceProvider {
        internal const string FileSettingsSectionName = "packageSources";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PackageSourceProvider Default = new PackageSourceProvider(Settings.UserSettings);

        private ISettings _settingsManager;

        public PackageSourceProvider(ISettings settingsManager) {
            if (settingsManager == null) {
                throw new ArgumentNullException("settingsManager");
            }

            _settingsManager = settingsManager;
        }

        public IEnumerable<PackageSource> LoadPackageSources() {
            IList<KeyValuePair<string, string>> settingsValue = _settingsManager.GetValues(FileSettingsSectionName);
            if (settingsValue != null && settingsValue.Count > 0) {
                return settingsValue.Select(p => new PackageSource(p.Value, p.Key)).ToList();
            }

            return Enumerable.Empty<PackageSource>();
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources) {
            // clear the old values
            _settingsManager.DeleteSection(FileSettingsSectionName);

            // and write the new ones
            _settingsManager.SetValues(
                FileSettingsSectionName,
                sources.Select(p => new KeyValuePair<string, string>(p.Name, p.Source)).ToList());
        }
    }
}
