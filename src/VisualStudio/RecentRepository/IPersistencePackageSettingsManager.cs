using System.Collections.Generic;

namespace NuGet.VisualStudio {

    /// <summary>
    /// Responsible for loading and storing package metadata to the VS settings store.
    /// </summary>
    public interface IPersistencePackageSettingsManager {

        /// <summary>
        /// Loads all package metadata from the settings store.
        /// </summary>
        IEnumerable<IPersistencePackageMetadata> LoadPackageMetadata(int maximumCount);

        /// <summary>
        /// Saves the specified package metadata to the settings store.
        /// </summary>
        void SavePackageMetadata(IEnumerable<IPersistencePackageMetadata> packageMetadata);

        /// <summary>
        /// Clear all package metadata from the settings store.
        /// </summary>
        void ClearPackageMetadata();
    }
}