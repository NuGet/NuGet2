
namespace NuGet.VisualStudio
{
    public interface IRecentPackageRepository : IPackageRepository
    {
        /// <summary>
        /// Clear all packages from the Recent list
        /// </summary>
        void Clear();

        /// <summary>
        /// Update the specified package in the Recent list if there is 
        /// already an older version of it in the Recent list. If there
        /// isn't an older version of it, do nothing.
        /// </summary>
        void UpdatePackage(IPackage package);
    }
}
