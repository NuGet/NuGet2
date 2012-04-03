
namespace NuGet
{
    public interface ISharedPackageRepository : IPackageRepository
    {
        bool IsReferenced(string packageId, SemanticVersion version);

        /// <summary>
        /// Adds an entry to the solution-level packages.config file
        /// </summary>
        /// <param name="packageId">The package id.</param>
        /// <param name="version">The version.</param>
        void AddPackageReferenceEntry(string packageId, SemanticVersion version);

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
