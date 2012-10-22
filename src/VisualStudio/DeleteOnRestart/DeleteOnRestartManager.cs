using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDeleteOnRestartManager))]
    public sealed class DeleteOnRestartManager : IDeleteOnRestartManager
    {
        // The file extension to add to the empty files which will be placed adjacent to partially uninstalled package
        // directories marking them for removal the next time the solution is opened.
        private const string DeletionMarkerSuffix = ".deleteme";
        private const string DeletionMarkerFilter = "*" + DeletionMarkerSuffix;

        private readonly Func<IFileSystem> _repositoryFileSystemFactory;
        private readonly Func<IPackagePathResolver> _packagePathResolverFactory;

        [ImportingConstructor]
        public DeleteOnRestartManager(IFileSystemProvider fileSystemProvider, IRepositorySettings repositorySettings)
            : this(() => fileSystemProvider.GetFileSystem(repositorySettings.RepositoryPath))
        {
        }

        public DeleteOnRestartManager(Func<IFileSystem> repositoryFileSystemFactory)
            : this(repositoryFileSystemFactory, () => new DefaultPackagePathResolver(repositoryFileSystemFactory()))
        {
        }

        internal DeleteOnRestartManager(Func<IFileSystem> repositoryFileSystemFactory, Func<IPackagePathResolver> packagePathResolverFactory)
        {
            _repositoryFileSystemFactory = repositoryFileSystemFactory;
            _packagePathResolverFactory = packagePathResolverFactory;
        }

        /// <summary>
        /// Indicates whether any packages still need to be deleted as indicated by the existence of .deleteme files
        /// in the local package repository.
        /// </summary>
        public bool PackageDirectoriesAreMarkedForDeletion
        {
            get { return _repositoryFileSystemFactory().GetFiles(path: "", filter: DeletionMarkerFilter, recursive: false).Any(); }
        }

        /// <summary>
        /// Marks package directory for future removal if it was not fully deleted during the normal uninstall process
        /// if the directory does not contain any added or modified files.
        /// The package directory will be marked by an adjacent *directory name*.deleteme file.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        public void MarkPackageDirectoryForDeletion(IPackage package, Func<string, IPackage> createZipPackageFromPath)
        {
            IFileSystem repositoryFileSystem = _repositoryFileSystemFactory();
            IPackagePathResolver pathResolver = _packagePathResolverFactory();
            string packageDirectoryName = pathResolver.GetPackageDirectory(package);
            
            try
            {
                if (repositoryFileSystem.DirectoryExists(packageDirectoryName))
                {
                    string packageFilePath = Path.Combine(packageDirectoryName, pathResolver.GetPackageFileName(package));
                    IPackage zipPackage = createZipPackageFromPath(repositoryFileSystem.GetFullPath(packageFilePath));

                    IEnumerable<IPackageFile> origPackageFiles = zipPackage.GetFiles();
                    IDictionary<string, IPackageFile> origPackageFileLookup = origPackageFiles.ToDictionary(f => Path.Combine(packageDirectoryName, f.Path), StringComparer.OrdinalIgnoreCase);

                    foreach (var filePath in repositoryFileSystem.GetFiles(path: packageDirectoryName, filter: "*.*", recursive: true))
                    {
                        // Assume the package file and the package manifest are unmodified.
                        if (!filePath.Equals(packageFilePath, StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetExtension(filePath).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            // Don't mark directory for deletion if file has been added.
                            IPackageFile origFile;
                            if (!origPackageFileLookup.TryGetValue(filePath, out origFile))
                            {
                                return;
                            }

                            // Don't mark directory for deletion if file has been modified.
                            using (Stream origStream = origFile.GetStream(),
                                          fileStream = repositoryFileSystem.OpenFile(filePath))
                            {
                                if (!origStream.ContentEquals(fileStream))
                                {
                                    return;
                                }
                            }
                        }
                    }

                    // NOTE: The repository should always be a PhysicalFileSystem, except during testing, so the
                    // .deleteme marker file doesn't get checked into version control
                    repositoryFileSystem.AddFile(packageDirectoryName + DeletionMarkerSuffix, Stream.Null);
                }
            }
            catch (Exception e)
            {
                repositoryFileSystem.Logger.Log(MessageLevel.Warning, String.Format(Resources.VsResources.Warning_FailedToMarkPackageDirectoryForDeletion, packageDirectoryName, e.Message));
            }
        }

        /// <summary>
        /// Attempts to remove package directories that were unable to be fully deleted during the original uninstall.
        /// These directories will be marked by an adjacent *directory name*.deleteme files in the local package repository.
        /// If the directory removal is successful, the .deleteme file will also be removed.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        public void DeleteMarkedPackageDirectories()
        {
            IFileSystem repositoryFileSystem = _repositoryFileSystemFactory();
            try
            {
                foreach (string deletemePath in repositoryFileSystem.GetFiles(path: "", filter: DeletionMarkerFilter, recursive: false))
                {
                    string deletedPackageDirectoryPath = Path.GetFileNameWithoutExtension(deletemePath);
                    try
                    {
                        // DeleteDirectory should not throw a DirectoryNotFoundException. It might throw an IOException, UnauthorizedAccessException, etc...
                        repositoryFileSystem.DeleteDirectory(deletedPackageDirectoryPath, recursive: true);
                    }
                    finally
                    {
                        if (!repositoryFileSystem.DirectoryExists(deletedPackageDirectoryPath))
                        {
                            repositoryFileSystem.DeleteFile(deletemePath);
                        }
                        else
                        {
                            repositoryFileSystem.Logger.Log(MessageLevel.Warning, String.Format(Resources.VsResources.Warning_FailedToDeleteMarkedPackageDirectory, deletedPackageDirectoryPath));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                repositoryFileSystem.Logger.Log(MessageLevel.Warning, String.Format(Resources.VsResources.Warning_FailedToDeleteMarkedPackageDirectories, e.Message));
            }
        }
    }
}
