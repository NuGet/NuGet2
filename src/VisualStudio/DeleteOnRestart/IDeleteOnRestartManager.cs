using System;

namespace NuGet.VisualStudio
{
    public interface IDeleteOnRestartManager
    {
        /// <summary>
        /// Indicates whether any packages still need to be deleted in the local package repository.
        /// </summary>
        bool PackageDirectoriesAreMarkedForDeletion { get; }

        /// <summary>
        /// Marks package directory for future removal if it was not fully deleted during the normal uninstall process
        /// if the directory does not contain any added or modified files.
        /// </summary>
        void MarkPackageDirectoryForDeletion(IPackage package, Func<string, IPackage> createZipPackageFromPath);

        /// <summary>
        /// Attempts to remove marked package directories that were unable to be fully deleted during the original uninstall.
        /// </summary>
        void DeleteMarkedPackageDirectories();
    }
}
