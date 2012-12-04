using System;
using System.Collections.Generic;

namespace NuGet.VisualStudio
{
    public interface IDeleteOnRestartManager
    {
        /// <summary>
        /// Gets the list of package directories that are still need to be deleted in the
        /// local package repository.
        /// </summary>
        IList<string> GetPackageDirectoriesMarkedForDeletion();

        /// <summary>
        /// Marks package directory for future removal if it was not fully deleted during the normal uninstall process
        /// if the directory does not contain any added or modified files.
        /// </summary>
        void MarkPackageDirectoryForDeletion(IPackage package);

        /// <summary>
        /// Attempts to remove marked package directories that were unable to be fully deleted during the original uninstall.
        /// </summary>
        void DeleteMarkedPackageDirectories();
    }
}