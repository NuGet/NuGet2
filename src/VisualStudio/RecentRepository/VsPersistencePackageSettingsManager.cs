using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;

namespace NuGet.VisualStudio {

    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IPersistencePackageSettingsManager))]
    public class VsPersistencePackageSettingsManager : SettingsManagerBase, IPersistencePackageSettingsManager {

        private const string MruSettingsRoot = "NuGet\\Mru";
        // template = NuGet\Mru\Package
        private const string SettingsRootTemplate = MruSettingsRoot + "\\Package";
        private static readonly string[] SettingsProperties = new string[] { "Id", "Version", "LastUsed" };

        public VsPersistencePackageSettingsManager()
            : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsPersistencePackageSettingsManager(IServiceProvider serviceProvider)
            : base(serviceProvider) {
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
                if (String.IsNullOrEmpty(values[0]) || String.IsNullOrEmpty(values[1])) {
                    continue;
                }

                DateTime lastUsedDate = ConvertFromStringToDateTime(values[2]);
                yield return new PersistencePackageMetadata(values[0], values[1], lastUsedDate);
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
        ///     Package2             | LastUsed: (date time in a long number)
        ///     Package3
        /// </remarks>
        public void SavePackageMetadata(IEnumerable<IPersistencePackageMetadata> packageMetadata) {
            if (packageMetadata == null) {
                throw new ArgumentNullException("packageMetadata");
            }

            int count = 0;
            foreach (var metadata in packageMetadata) {
                string settingsRoot = SettingsRootTemplate + count.ToString(CultureInfo.InvariantCulture);
                string[] values = new string[] {
                    metadata.Id, 
                    metadata.Version.ToString(), 
                    ConvertFromDateTimeToString(metadata.LastUsedDate)
                };
                WriteStrings(settingsRoot, SettingsProperties, values);
                count++;
            }
        }

        private static string ConvertFromDateTimeToString(DateTime dateTime) {
            // This is suggested by MSDN to serialize and deserialize date time
            // http://msdn.microsoft.com/en-us/library/system.datetime.tobinary.aspx
            return dateTime.ToBinary().ToString(CultureInfo.InvariantCulture);
        }

        private static DateTime ConvertFromStringToDateTime(string value) {
            if (!String.IsNullOrEmpty(value)) {
                long binaryData;
                if (Int64.TryParse(value, out binaryData)) {
                    return DateTime.FromBinary(binaryData);
                }
            }

            return DateTime.MinValue;
        }

        public void ClearPackageMetadata() {
            // delete everything under NuGet\Mru
            ClearAllSettings(MruSettingsRoot);
        }
    }
}