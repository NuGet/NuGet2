using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace NuGet
{
    public class PackageSourceProvider : IPackageSourceProvider
    {
        private const int MaxSupportedProtocolVersion = 2;
        private const string PackageSourcesSectionName = "packageSources";
        private const string DisabledPackageSourcesSectionName = "disabledPackageSources";
        private const string CredentialsSectionName = "packageSourceCredentials";
        private const string UsernameToken = "Username";
        private const string PasswordToken = "Password";
        private const string ClearTextPasswordToken = "ClearTextPassword";
        private const string ProtocolVersionAttribute = "protocolVersion";
        private readonly ISettings _settingsManager;
        private readonly IEnumerable<PackageSource> _providerDefaultSources;
        private readonly IDictionary<PackageSource, PackageSource> _migratePackageSources;
        private readonly IEnumerable<PackageSource> _configurationDefaultSources;

        public PackageSourceProvider(ISettings settingsManager)
            : this(settingsManager, providerDefaultSources: null)
        {
        }

        /// <summary>
        /// Creates a new PackageSourceProvider instance.
        /// </summary>
        /// <param name="settingsManager">Specifies the settings file to use to read package sources.</param>
        /// <param name="providerDefaultSources">Specifies the default sources to be used as per the PackageSourceProvider. These are always loaded
        /// Default Feeds from PackageSourceProvider are generally the feeds from the NuGet Client like the NuGetOfficialFeed from the Visual Studio client for NuGet</param>
        public PackageSourceProvider(ISettings settingsManager, IEnumerable<PackageSource> providerDefaultSources)
            : this(settingsManager, providerDefaultSources, migratePackageSources: null)
        {
        }

        public PackageSourceProvider(
            ISettings settingsManager,
            IEnumerable<PackageSource> providerDefaultSources,
            IDictionary<PackageSource, PackageSource> migratePackageSources)
            : this(settingsManager, providerDefaultSources, migratePackageSources, ConfigurationDefaults.Instance.DefaultPackageSources)
        {

        }

        internal PackageSourceProvider(
            ISettings settingsManager,
            IEnumerable<PackageSource> providerDefaultSources,
            IDictionary<PackageSource, PackageSource> migratePackageSources,
            IEnumerable<PackageSource> configurationDefaultSources)
        {
            if (settingsManager == null)
            {
                throw new ArgumentNullException("settingsManager");
            }
            _settingsManager = settingsManager;
            _providerDefaultSources = providerDefaultSources ?? Enumerable.Empty<PackageSource>();
            _migratePackageSources = migratePackageSources;
            _configurationDefaultSources = configurationDefaultSources ?? Enumerable.Empty<PackageSource>();
        }

        /// <summary>
        /// Returns PackageSources if specified in the config file. Else returns the default sources specified in the constructor.
        /// If no default values were specified, returns an empty sequence.
        /// </summary>
        public IEnumerable<PackageSource> LoadPackageSources()
        {
            IList<SettingValue> sourceSettingValues = _settingsManager.GetValues(PackageSourcesSectionName, isPath: true) ??
                new SettingValue[0];

            // Order the list so that they are ordered in priority order
            var settingValues = sourceSettingValues.OrderByDescending(setting => setting.Priority);

            // get list of disabled packages
            var disabledSetting = _settingsManager.GetValues(DisabledPackageSourcesSectionName, isPath: false) ?? Enumerable.Empty<SettingValue>();

            var disabledSources = new Dictionary<string, SettingValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var setting in disabledSetting)
            {
                if (disabledSources.ContainsKey(setting.Key))
                {
                    disabledSources[setting.Key] = setting;
                }
                else
                {
                    disabledSources.Add(setting.Key, setting);
                }
            }

            var packageSourceLookup = new Dictionary<string, IndexedPackageSource>(StringComparer.OrdinalIgnoreCase);
            var packageIndex = 0;
            foreach (var setting in settingValues)
            {
                var name = setting.Key;

                bool isEnabled = true;
                SettingValue disabledSource;
                if (disabledSources.TryGetValue(name, out disabledSource) &&
                    disabledSource.Priority >= setting.Priority)
                {
                    isEnabled = false;
                }

                var packageSource = ReadPackageSource(setting, isEnabled);
                if (packageSource.ProtocolVersion <= MaxSupportedProtocolVersion)
                {
                    packageIndex = AddOrUpdateIndexedSource(packageSourceLookup, packageIndex, packageSource);
                }
            }

            var loadedPackageSources = packageSourceLookup.Values
                .OrderBy(source => source.Index)
                .Select(source => source.PackageSource)
                .ToList();

            if (_migratePackageSources != null)
            {
                MigrateSources(loadedPackageSources);
            }

            SetDefaultPackageSources(loadedPackageSources);

            return loadedPackageSources;
        }

        private PackageSourceCredential ReadCredential(string sourceName)
        {
            var values = _settingsManager.GetNestedValues(CredentialsSectionName, sourceName);
            if (!values.IsEmpty())
            {
                string userName = values.FirstOrDefault(k => k.Key.Equals(UsernameToken, StringComparison.OrdinalIgnoreCase)).Value;

                if (!String.IsNullOrEmpty(userName))
                {
                    var setting = values.FirstOrDefault(k => k.Key.Equals(PasswordToken, StringComparison.OrdinalIgnoreCase));
                    string encryptedPassword = setting != null ? setting.Value : null;
                    if (!String.IsNullOrEmpty(encryptedPassword))
                    {
                        return new PackageSourceCredential(userName, EncryptionUtility.DecryptString(encryptedPassword), isPasswordClearText: false);
                    }

                    setting = values.FirstOrDefault(k => k.Key.Equals(ClearTextPasswordToken, StringComparison.Ordinal));
                    string clearTextPassword = setting != null ? setting.Value : null;
                    if (!String.IsNullOrEmpty(clearTextPassword))
                    {
                        return new PackageSourceCredential(userName, clearTextPassword, isPasswordClearText: true);
                    }
                }
            }
            return null;
        }

        private void MigrateSources(List<PackageSource> loadedPackageSources)
        {
            bool hasChanges = false;
            List<PackageSource> packageSourcesToBeRemoved = new List<PackageSource>();

            // doing migration
            for (int i = 0; i < loadedPackageSources.Count; i++)
            {
                PackageSource ps = loadedPackageSources[i];
                PackageSource targetPackageSource;
                if (_migratePackageSources.TryGetValue(ps, out targetPackageSource))
                {
                    if (loadedPackageSources.Any(p => p.Equals(targetPackageSource)))
                    {
                        packageSourcesToBeRemoved.Add(loadedPackageSources[i]);
                    }
                    else
                    {
                        loadedPackageSources[i] = targetPackageSource.Clone();
                        // make sure we preserve the IsEnabled property when migrating package sources
                        loadedPackageSources[i].IsEnabled = ps.IsEnabled;
                    }
                    hasChanges = true;
                }
            }

            foreach (PackageSource packageSource in packageSourcesToBeRemoved)
            {
                loadedPackageSources.Remove(packageSource);
            }

            if (hasChanges)
            {
                SavePackageSources(loadedPackageSources);
            }
        }

        private void SetDefaultPackageSources(List<PackageSource> loadedPackageSources)
        {
            // There are 4 different cases to consider for default package sources
            // Case 1. Default Package Source is already present matching both feed source and the feed name
            // Case 2. Default Package Source is already present matching feed source but with a different feed name. DO NOTHING
            // Case 3. Default Package Source is not present, but there is another feed source with the same feed name. Override that feed entirely
            // Case 4. Default Package Source is not present, simply, add it

            IEnumerable<PackageSource> allDefaultPackageSources = _configurationDefaultSources;

            if (allDefaultPackageSources.IsEmpty<PackageSource>())
            {
                // Update provider default sources and use provider default sources since _configurationDefaultSources is empty
                UpdateProviderDefaultSources(loadedPackageSources);
                allDefaultPackageSources = _providerDefaultSources;
            }

            var defaultPackageSourcesToBeAdded = new List<PackageSource>();
            foreach (PackageSource packageSource in allDefaultPackageSources)
            {
                int sourceMatchingIndex = loadedPackageSources.FindIndex(p => p.Source.Equals(packageSource.Source, StringComparison.OrdinalIgnoreCase));
                if (sourceMatchingIndex != -1)
                {
                    if (loadedPackageSources[sourceMatchingIndex].Name.Equals(packageSource.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Case 1: Both the feed name and source matches. DO NOTHING except set IsOfficial to true
                        loadedPackageSources[sourceMatchingIndex].IsOfficial = true;
                    }
                    else
                    {
                        // Case 2: Only feed source matches but name is different. DO NOTHING
                    }
                }
                else
                {
                    int nameMatchingIndex = loadedPackageSources.FindIndex(p => p.Name.Equals(packageSource.Name, StringComparison.CurrentCultureIgnoreCase));
                    if (nameMatchingIndex != -1)
                    {
                        // Case 3: Only feed name matches but source is different. Override it entirely
                        loadedPackageSources[nameMatchingIndex] = packageSource;
                    }
                    else
                    {
                        // Case 4: Default package source is not present. Add it to the temp list. Later, the temp listed is inserted above the machine wide sources
                        defaultPackageSourcesToBeAdded.Add(packageSource);
                    }
                }
            }

            var defaultSourcesInsertIndex = loadedPackageSources.FindIndex(source => source.IsMachineWide);
            if (defaultSourcesInsertIndex == -1)
            {
                defaultSourcesInsertIndex = loadedPackageSources.Count;
            }

            // Default package sources go ahead of machine wide sources
            loadedPackageSources.InsertRange(defaultSourcesInsertIndex, defaultPackageSourcesToBeAdded);
        }

        private void UpdateProviderDefaultSources(List<PackageSource> loadedSources)
        {
            // If there are NO other non-machine wide sources, providerDefaultSource should be enabled
            bool areProviderDefaultSourcesEnabled = loadedSources.Count == 0 || loadedSources.Where(p => !p.IsMachineWide).Count() == 0;

            foreach (PackageSource packageSource in _providerDefaultSources)
            {
                packageSource.IsEnabled = areProviderDefaultSourcesEnabled;
                packageSource.IsOfficial = true;
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502", Justification = "This is ported from NuGet3 and we want to keep the implementations in sync.")]
        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            // clear the old values
            // and write the new ones
            var sourcesToWrite = sources.Where(s => !s.IsMachineWide);

            var existingSettings = (_settingsManager.GetValues(PackageSourcesSectionName, isPath: true) ??
                Enumerable.Empty<SettingValue>()).Where(setting => !setting.IsMachineWide).ToList();

            var existingSettingsLookup = existingSettings.ToLookup(setting => setting.Key, StringComparer.OrdinalIgnoreCase);
            var existingDisabledSources = _settingsManager.GetValues(DisabledPackageSourcesSectionName, isPath: false) ??
                Enumerable.Empty<SettingValue>();
            var existingDisabledSourcesLookup = existingDisabledSources.ToLookup(setting => setting.Key, StringComparer.OrdinalIgnoreCase);

            var sourceSettings = new List<SettingValue>();
            var sourcesToDisable = new List<SettingValue>();

            foreach (var source in sourcesToWrite)
            {
                var foundSettingWithSourcePriority = false;
                var settingPriority = 0;
                var existingSettingForSource = existingSettingsLookup[source.Name];

                // Preserve packageSource entries from low priority settings.
                foreach (var existingSetting in existingSettingForSource)
                {
                    settingPriority = Math.Max(settingPriority, existingSetting.Priority);

                    // Write all settings other than the currently written one to the current NuGet.config.
                    if (ReadProtocolVersion(existingSetting) == source.ProtocolVersion)
                    {
                        // Update the source value of all settings with the same protocol version.
                        existingSetting.Value = source.Source;
                        foundSettingWithSourcePriority = true;
                    }
                    sourceSettings.Add(existingSetting);
                }

                if (!foundSettingWithSourcePriority)
                {
                    // This is a new source, add it to the Setting with the lowest priority.
                    var settingValue = new SettingValue(source.Name, source.Source, isMachineWide: false);
                    if (source.ProtocolVersion != PackageSource.DefaultProtocolVersion)
                    {
                        settingValue.AdditionalData[ProtocolVersionAttribute] =
                            source.ProtocolVersion.ToString(CultureInfo.InvariantCulture);
                    }

                    sourceSettings.Add(settingValue);
                }

                // settingValue contains the setting with the highest priority.

                var existingDisabledSettings = existingDisabledSourcesLookup[source.Name];
                // Preserve disabledPackageSource entries from low priority settings.
                foreach (var setting in existingDisabledSettings.Where(s => s.Priority < settingPriority))
                {
                    sourcesToDisable.Add(setting);
                }

                if (!source.IsEnabled)
                {
                    // Add an entry to the disabledPackageSource in the file that contains
                    sourcesToDisable.Add(new SettingValue(source.Name, "true", isMachineWide: false, priority: settingPriority));
                }
            }

            // Re-add all settings with a higher protocol version that weren't listed. Skip any settings where the source is
            // already being written and any setting with any source with a protocol version < 2 (the max supported version).
            // The latter indicates a deleted source.
            var sourcesWithHigherProtocolVersion = existingSettingsLookup
                .Where(item =>
                    !sourcesToWrite.Any(s => string.Equals(s.Name, item.Key, StringComparison.OrdinalIgnoreCase)) &&
                    !item.Any(s => ReadProtocolVersion(s) <= MaxSupportedProtocolVersion))
                .SelectMany(s => s);
            sourceSettings.AddRange(sourcesWithHigherProtocolVersion);

            // Add disabled machine wide sources
            foreach (var source in sources.Where(s => s.IsMachineWide && !s.IsEnabled))
            {
                sourcesToDisable.Add(new SettingValue(source.Name, "true", isMachineWide: false));
            }

            // Write the updates to the nearest settings file.
            _settingsManager.UpdateSections(PackageSourcesSectionName, sourceSettings);

            // overwrite new values for the <disabledPackageSources> section
            _settingsManager.UpdateSections(DisabledPackageSourcesSectionName, sourcesToDisable);

            // Overwrite the <packageSourceCredentials> section
            _settingsManager.DeleteSection(CredentialsSectionName);

            var sourceWithCredentials = sources.Where(s => !String.IsNullOrEmpty(s.UserName) && !String.IsNullOrEmpty(s.Password));
            foreach (var source in sourceWithCredentials)
            {
                _settingsManager.SetNestedValues(CredentialsSectionName, source.Name, new[] {
                    new KeyValuePair<string, string>(UsernameToken, source.UserName),
                    ReadPasswordValues(source)
                });
            }
        }

        private static KeyValuePair<string, string> ReadPasswordValues(PackageSource source)
        {
            string passwordToken = source.IsPasswordClearText ? ClearTextPasswordToken : PasswordToken;
            string passwordValue = source.IsPasswordClearText ? source.Password : EncryptionUtility.EncryptString(source.Password);

            return new KeyValuePair<string, string>(passwordToken, passwordValue);
        }

        public void DisablePackageSource(PackageSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            _settingsManager.SetValue(DisabledPackageSourcesSectionName, source.Name, "true");
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            string value = _settingsManager.GetValue(
                DisabledPackageSourcesSectionName,
                source.Name,
                isPath: false);

            // It doesn't matter what value it is.
            // As long as the package source name is persisted in the <disabledPackageSources> section, the source is disabled.
            return String.IsNullOrEmpty(value);
        }

        private PackageSource ReadPackageSource(SettingValue setting, bool isEnabled)
        {
            var name = setting.Key;
            var packageSource = new PackageSource(setting.Value, name, isEnabled)
            {
                IsMachineWide = setting.IsMachineWide
            };

            var credentials = ReadCredential(name);
            if (credentials != null)
            {
                packageSource.UserName = credentials.Username;
                packageSource.Password = credentials.Password;
                packageSource.IsPasswordClearText = credentials.IsPasswordClearText;
            }

            packageSource.ProtocolVersion = ReadProtocolVersion(setting);

            return packageSource;
        }

        private static int ReadProtocolVersion(SettingValue setting)
        {
            string protocolVersionString;
            int protocolVersion;
            if (setting.AdditionalData.TryGetValue(ProtocolVersionAttribute, out protocolVersionString) &&
                int.TryParse(protocolVersionString, out protocolVersion))
            {
                return protocolVersion;
            }

            return PackageSource.DefaultProtocolVersion;
        }

        private static int AddOrUpdateIndexedSource(
            Dictionary<string, IndexedPackageSource> packageSourceLookup,
            int packageIndex,
            PackageSource packageSource)
        {
            IndexedPackageSource previouslyAddedSource;
            if (!packageSourceLookup.TryGetValue(packageSource.Name, out previouslyAddedSource))
            {
                packageSourceLookup[packageSource.Name] = new IndexedPackageSource
                {
                    PackageSource = packageSource,
                    Index = packageIndex++
                };
            }
            else if (previouslyAddedSource.PackageSource.ProtocolVersion < packageSource.ProtocolVersion)
            {
                // Pick the package source with the highest supported protocol version
                previouslyAddedSource.PackageSource = packageSource;
            }

            return packageIndex;
        }


        private class PackageSourceCredential
        {
            public string Username { get; private set; }
            public string Password { get; private set; }
            public bool IsPasswordClearText { get; private set; }

            public PackageSourceCredential(string username, string password, bool isPasswordClearText)
            {
                Username = username;
                Password = password;
                IsPasswordClearText = isPasswordClearText;
            }
        }

        private class IndexedPackageSource
        {
            public int Index { get; set; }

            public PackageSource PackageSource { get; set; }
        }
    }
}