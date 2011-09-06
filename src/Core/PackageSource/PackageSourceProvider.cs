using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet {
    public class PackageSourceProvider : IPackageSourceProvider {
        internal const string FileSettingsSectionName = "packageSources";
        private readonly ISettings _settingsManager;
        private readonly IEnumerable<PackageSource> _defaultPackageSources;

        public PackageSourceProvider(ISettings settingsManager)
            : this(settingsManager, defaultSources: null) {
        }

        /// <summary>
        /// Creates a new PackageSourceProvider instance.
        /// </summary>
        /// <param name="settingsManager">Specifies the settings file to use to read package sources.</param>
        /// <param name="defaultSources">Specifies the sources to return if no package sources are available.</param>
        public PackageSourceProvider(ISettings settingsManager, IEnumerable<PackageSource> defaultSources) {
            if (settingsManager == null) {
                throw new ArgumentNullException("settingsManager");
            }
            _settingsManager = settingsManager;
            _defaultPackageSources = defaultSources ?? Enumerable.Empty<PackageSource>();
        }

        /// <summary>
        /// Returns PackageSources if specified in the config file. Else returns the default sources specified in the constructor.
        /// If no default values were specified, returns an empty sequence.
        /// </summary>
        public IEnumerable<PackageSource> LoadPackageSources() {
            IList<KeyValuePair<string, string>> settingsValue = _settingsManager.GetValues(FileSettingsSectionName);
            if (settingsValue != null && settingsValue.Any()) {
                return settingsValue.Select(p => new PackageSource(p.Value, p.Key)).ToList();
            }
            return _defaultPackageSources;
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
