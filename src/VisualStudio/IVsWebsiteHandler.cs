using System.Collections.Generic;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public interface IVsWebsiteHandler
    {
        /// <summary>
        /// Adds refresh files to the specified project for all assemblies references belonging to the packages specified by packageNames.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="packagesFileSystem">The file system pointing to 'packages' folder under the solution.</param>
        /// <param name="packageNames">The package names.</param>
        void AddRefreshFilesForReferences(Project project, IFileSystem packagesFileSystem, IEnumerable<PackageName> packageNames);

        /// <summary>
        /// Copies the native binaries to the project's bin folder.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="packagesFileSystem">The packages file system.</param>
        /// <param name="packageNames">The package names.</param>
        void CopyNativeBinaries(Project project, IFileSystem packagesFileSystem, IEnumerable<PackageName> packageNames);
    }
}