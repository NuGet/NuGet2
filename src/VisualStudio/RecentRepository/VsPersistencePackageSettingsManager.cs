using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPersistencePackageSettingsManager))]
    public class VsPersistencePackageSettingsManager : SettingsManagerBase, IPersistencePackageSettingsManager {

        private const string MruSettingsRoot = "NuGet\\Mru";
        // template = NuGet\Mru\Package
        private const string SettingsRootTemplate = MruSettingsRoot + "\\Package";
        private static readonly string[] SettingsProperties = new string[] { "Id", "Version" };

        public VsPersistencePackageSettingsManager()
            : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsPersistencePackageSettingsManager(IServiceProvider serviceProvider)
            : base(serviceProvider) {
        }

        public IEnumerable<PersistencePackageMetadata> LoadPackageMetadata(int maximumCount) {
            for (int i = 0; i < maximumCount; i++) {
                string settingsRoot = SettingsRootTemplate + i.ToString(CultureInfo.InvariantCulture);
                string[] values = ReadStrings(settingsRoot, SettingsProperties);

                // if we can't read a particular package, it means there's no more.
                if (values == null) {
                    yield break;
                }

                // avoid corrupted data
                if (values.Length != 2 || values.Any(p => String.IsNullOrEmpty(p))) {
                    continue;
                }

                yield return new PersistencePackageMetadata(values[0], values[1]);
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
        ///     Package2             
        ///     Package3
        /// </remarks>
        public void SavePackageMetadata(IEnumerable<PersistencePackageMetadata> packageMetadata) {
            if (packageMetadata == null) {
                throw new ArgumentNullException("packageMetadata");
            }

            int count = 0;
            foreach (var metadata in packageMetadata) {
                string settingsRoot = SettingsRootTemplate + count.ToString(CultureInfo.InvariantCulture);
                string[] values = new string[] { metadata.Id, metadata.Version.ToString()};
                WriteStrings(settingsRoot, SettingsProperties, values);
                count++;
            }
        }

        public void ClearPackageMetadata() {
            // delete everything under NuGet\Mru
            ClearAllSettings(MruSettingsRoot);
        }
    }
}