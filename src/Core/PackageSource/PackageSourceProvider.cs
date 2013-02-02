using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    public class PackageSourceProvider : IPackageSourceProvider
    {
        private const string PackageSourcesSectionName = "packageSources";
        private const string DisabledPackageSourcesSectionName = "disabledPackageSources";
        private const string CredentialsSectionName = "packageSourceCredentials";
        private const string UsernameToken = "Username";
        private const string PasswordToken = "Password";
        private const string ClearTextPasswordToken = "ClearTextPassword";
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
            if (!settingsValue.IsEmpty())
            {
                // put disabled package source names into the hash set

                IEnumerable<KeyValuePair<string, string>> disabledSourcesValues = _settingsManager.GetValues(DisabledPackageSourcesSectionName) ??
                                                                                  Enumerable.Empty<KeyValuePair<string, string>>();
                var disabledSources = new HashSet<string>(disabledSourcesValues.Select(s => s.Key), StringComparer.CurrentCultureIgnoreCase);
                var loadedPackageSources = settingsValue.
                                           Select(p =>
                                           {
                                               string name = p.Key;
                                               string src = p.Value;
                                               PackageSourceCredential creds = ReadCredential(name);

                                               // We always set the isOfficial bit to false here, as Core doesn't have the concept of an official package source.
                                               return new PackageSource(src, name, isEnabled: !disabledSources.Contains(name), isOfficial: false)
                                               {
                                                   UserName = creds != null ? creds.Username : null,
                                                   Password = creds != null ? creds.Password : null,
                                                   IsPasswordClearText = creds != null && creds.IsPasswordClearText
                                               };

                                           }).ToList();

                if (_migratePackageSources != null)
                {
                    MigrateSources(loadedPackageSources);
                }

                return loadedPackageSources;
            }
            return _defaultPackageSources;
        }

        private PackageSourceCredential ReadCredential(string sourceName)
        {
            var values = _settingsManager.GetNestedValues(CredentialsSectionName, sourceName);
            if (!values.IsEmpty())
            {
                string userName = values.FirstOrDefault(k => k.Key.Equals(UsernameToken, StringComparison.OrdinalIgnoreCase)).Value;

                if (!String.IsNullOrEmpty(userName))
                {
                    string encryptedPassword = values.FirstOrDefault(k => k.Key.Equals(PasswordToken, StringComparison.OrdinalIgnoreCase)).Value;
                    if (!String.IsNullOrEmpty(encryptedPassword))
                    {
                        return new PackageSourceCredential(userName, EncryptionUtility.DecryptString(encryptedPassword), isPasswordClearText: false);
                    }

                    string clearTextPassword = values.FirstOrDefault(k => k.Key.Equals(ClearTextPasswordToken, StringComparison.Ordinal)).Value;
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
            // doing migration
            for (int i = 0; i < loadedPackageSources.Count; i++)
            {
                PackageSource ps = loadedPackageSources[i];
                PackageSource targetPackageSource;
                if (_migratePackageSources.TryGetValue(ps, out targetPackageSource))
                {
                    loadedPackageSources[i] = targetPackageSource;
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

            string value = _settingsManager.GetValue(DisabledPackageSourcesSectionName, source.Name);

            // It doesn't matter what value it is.
            // As long as the package source name is persisted in the <disabledPackageSources> section, the source is disabled.
            return String.IsNullOrEmpty(value);
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
    }
}