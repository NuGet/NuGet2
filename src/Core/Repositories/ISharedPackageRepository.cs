using System;

namespace NuGet {
    public interface ISharedPackageRepository : IPackageRepository {
        bool IsReferenced(string packageId, Version version);

        /// <summary>
        /// Registers a new repository for the shared repository
        /// </summary>
        void RegisterRepository(string path);

        /// <summary>
        /// Removes a registered repository
        /// </summary>
        void UnregisterRepository(string path);
    }
}
