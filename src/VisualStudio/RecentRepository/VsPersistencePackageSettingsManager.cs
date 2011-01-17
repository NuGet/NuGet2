using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPersistencePackageSettingsManager))]
    public class VsPersistencePackageSettingsManager : SettingsManagerBase, IPersistencePackageSettingsManager {

        private const string SettingsRootTemplate = "NuGet\\Mru\\Package";
        private static readonly string[] SettingsProperties = new string[] { "Id", "Version", "Source" };

        [ImportingConstructor]
        public VsPersistencePackageSettingsManager(IServiceProvider serviceProvider) : base(serviceProvider) {
        }

        public IEnumerable<IPersistencePackageMetadata> LoadPackageMetadata(int maximumCount) {
            for (int i = 0; i < maximumCount; i++) {
                string settingsRoot = SettingsRootTemplate + i.ToString(CultureInfo.InvariantCulture);
                string[] values = ReadStrings(settingsRoot, SettingsProperties);

                // if we can't read a particular package, it means there's no more.
                if (values == null) {
                    yield break;
                }

                // avoid corrupted data
                if (values.Any(p => String.IsNullOrEmpty(p))) {
                    continue;
                }

                yield return new PersistencePackageMetadata(values[0], values[1], values[2]);
            }
        }

        /// <summary>
        /// Save the specified package metadata to VS settings store, which is the registry.
        /// </summary>
        /// <remarks>
        /// Here is how we save the data in the registry:
        /// 
        /// NuGet
        ///   Mru
        ///     Package0  ------->   | Id: Moq
        ///     Package1             | Version: 1.0.0.0 
        ///     Package2             | Source: http://....
        ///     Package3
        /// </remarks>
        public void SavePackageMetadata(IEnumerable<IPersistencePackageMetadata> packageMetadata) {
            if (packageMetadata == null) {
                throw new ArgumentNullException("packageMetadata");
            }

            int count = 0;
            foreach (var metadata in packageMetadata) {
                string settingsRoot = SettingsRootTemplate + count.ToString(CultureInfo.InvariantCulture);
                string[] values = new string[] { metadata.Id, metadata.Version.ToString(), metadata.Source };
                WriteStrings(settingsRoot, SettingsProperties, values);
                count++;
            }
        }

        private class PersistencePackageMetadata : IPersistencePackageMetadata {

            public PersistencePackageMetadata(string id, string version, string source) {
                Id = id;
                Version = new Version(version);
                Source = source;
            }

            public string Id { get; private set; }
            public Version Version { get; private set; }
            public string Source { get; private set; }
        }
    }
}
