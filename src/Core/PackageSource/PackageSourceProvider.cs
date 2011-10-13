using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    public class PackageSourceProvider : IPackageSourceProvider
    {
        internal const string PackageSourcesSectionName = "packageSources";
        internal const string DisabledPackageSourcesSectionName = "disabledPackageSources";
        private readonly ISettings _settingsManager;
        private readonly IEnumerable<PackageSource> _defaultPackageSources;
        private readonly IDictionary<PackageSource, PackageSource> _migratePackageSources;

        public PackageSourceProvider(ISettings settingsManager)
            : this(settingsManager, defaultSources: null)
        {
        }

        /// <summary>
        /// Creates a new PackageSourceProvider instance.
        /// </summary>
        /// <param name="settingsManager">Specifies the settings file to use to read package sources.</param>
        /// <param name="defaultSources">Specifies the sources to return if no package sources are available.</param>
        public PackageSourceProvider(ISettings settingsManager, IEnumerable<PackageSource> defaultSources)
            : this(settingsManager, defaultSources, migratePackageSources: null)
        {
        }

        public PackageSourceProvider(
            ISettings settingsManager,
            IEnumerable<PackageSource> defaultSources,
            IDictionary<PackageSource, PackageSource> migratePackageSources)
        {
            if (settingsManager == null)
            {
                throw new ArgumentNullException("settingsManager");
            }
            _settingsManager = settingsManager;
            _defaultPackageSources = defaultSources ?? Enumerable.Empty<PackageSource>();
            _migratePackageSources = migratePackageSources;
        }

        /// <summary>
        /// Returns PackageSources if specified in the config file. Else returns the default sources specified in the constructor.
        /// If no default values were specified, returns an empty sequence.
        /// </summary>
        public IEnumerable<PackageSource> LoadPackageSources()
        {
            IList<KeyValuePair<string, string>> settingsValue = _settingsManager.GetValues(PackageSourcesSectionName);
            if (settingsValue != null && settingsValue.Any())
            {
                // put disabled package source names into the hash set
                var disabledSources = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                IList<KeyValuePair<string, string>> disabledSourcesValues = _settingsManager.GetValues(DisabledPackageSourcesSectionName);
                if (disabledSourcesValues != null)
                {
                    foreach (var pair in disabledSourcesValues)
                    {
                        disabledSources.Add(pair.Key);
                    }
                }

                var loadedPackageSources = settingsValue.
                                           Select(p => new PackageSource(p.Value, p.Key, isEnabled: !disabledSources.Contains(p.Key))).
                                           ToList();

                if (_migratePackageSources != null)
                {
                    bool hasChanges = false;
                    // doing migration
                    for (int i = 0; i < loadedPackageSources.Count; i++)
                    {
                        PackageSource ps = loadedPackageSources[i];
                        if (_migratePackageSources.ContainsKey(ps))
                        {
                            loadedPackageSources[i] = _migratePackageSources[ps];
                            // make sure we preserve the IsEnabled property when migrating package sources
                            loadedPackageSources[i].IsEnabled = ps.IsEnabled;
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        SavePackageSources(loadedPackageSources);
                    }
                }

                return loadedPackageSources;
            }
            return _defaultPackageSources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            // clear the old values
            _settingsManager.DeleteSection(PackageSourcesSectionName);

            // and write the new ones
            _settingsManager.SetValues(
                PackageSourcesSectionName,
                sources.Select(p => new KeyValuePair<string, string>(p.Name, p.Source)).ToList());

            // overwrite new values for the <disabledPackageSources> section
            _settingsManager.DeleteSection(DisabledPackageSourcesSectionName);

            _settingsManager.SetValues(
                DisabledPackageSourcesSectionName,
                sources.Where(p => !p.IsEnabled).Select(p => new KeyValuePair<string, string>(p.Name, "true")).ToList());
        }
    }
}