using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsWebsiteHandler))]
    public class VsWebsiteHandler : IVsWebsiteHandler
    {
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IProjectSystemFactory _projectSystemFactory;

        [ImportingConstructor]
        public VsWebsiteHandler(IFileSystemProvider fileSystemProvider)
        {
            _fileSystemProvider = fileSystemProvider;
        }

        /// <summary>
        /// This constructor is used by unit tests to inject custom IVsProjectSystem implementation
        /// </summary>
        internal VsWebsiteHandler(IFileSystemProvider fileSystemProvider, IProjectSystemFactory projectSystemFactory)
            : this(fileSystemProvider)
        {
            _projectSystemFactory = projectSystemFactory;
        }

        /// <summary>
        /// Adds refresh files to the specified project for all assemblies references belonging to the packages specified by packageNames.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="packagesFileSystem">The file system pointing to 'packages' folder under the solution.</param>
        /// <param name="packageNames">The package names.</param>
        public void AddRefreshFilesForReferences(Project project, IFileSystem packagesFileSystem, IEnumerable<PackageName> packageNames)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            if (packagesFileSystem == null)
            {
                throw new ArgumentNullException("packagesFileSystem");
            }

            if (packageNames.IsEmpty())
            {
                return;
            }

            FrameworkName projectTargetFramework = GetTargetFramework(project);

            var projectSystem = _projectSystemFactory != null ? _projectSystemFactory.CreateProjectSystem(project, _fileSystemProvider)
                                                              :  VsProjectSystemFactory.CreateProjectSystem(project, _fileSystemProvider);

            foreach (PackageName packageName in packageNames)
            {
                string packageDirectory;
                IEnumerable<IPackageAssemblyReference> assemblyReferences = GetAssemblyReferences(
                    packagesFileSystem, packageName.Id, packageName.Version, out packageDirectory);
                
                // get compatible assembly references for the current project's target framework
                IEnumerable<IPackageAssemblyReference> compatibleAssemblyReferences;
                if (VersionUtility.TryGetCompatibleItems(projectTargetFramework, assemblyReferences, out compatibleAssemblyReferences))
                {
                    foreach (var assemblyReference in compatibleAssemblyReferences)
                    {
                        // create one refresh file for each assembly reference, as per required by Website projects
                        CreateRefereshFile(projectSystem, assemblyReference, packageDirectory);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all assembly references for a package
        /// </summary>
        private IEnumerable<IPackageAssemblyReference> GetAssemblyReferences(
            IFileSystem fileSystem, string packageId, SemanticVersion version, out string packageDirectory)
        {
            // REVIEW: do we need to search for all variations of versions here? (e.g. 1.0, 1.0.0, 1.0.0.0)
            string packageName = packageId + "." + version.ToString();
            if (fileSystem.DirectoryExists(packageName))
            {
                string libFolderPath = Path.Combine(packageName, Constants.LibDirectory);
                if (fileSystem.DirectoryExists(libFolderPath))
                {
                    packageDirectory = fileSystem.GetFullPath(packageName);
                    return Constants.AssemblyReferencesExtensions
                                    .Select(extension => "*" + extension)
                                    .SelectMany(extension => SearchFilesWithinOneSubFolders(fileSystem, libFolderPath, extension))
                                    .Select(assembly => new FileAssemblyReference(assembly.Substring(packageName.Length).Trim(Path.DirectorySeparatorChar), fileSystem));
                }
            }

            packageDirectory = null;
            return Enumerable.Empty<IPackageAssemblyReference>();
        }

        private IEnumerable<string> SearchFilesWithinOneSubFolders(IFileSystem fileSystem, string folder, string extension)
        {
            // get files directly under 'folder' or files under subfolders of 'folder'
            return fileSystem.GetFiles(folder, extension)
                             .Concat(fileSystem.GetDirectories(folder).SelectMany(subFolder => fileSystem.GetFiles(subFolder, extension)));
        }

        private void CreateRefereshFile(IProjectSystem project, IPackageAssemblyReference assemblyReference, string packageInstallPath)
        {
            string refreshFilePath = Path.Combine("bin", assemblyReference.Name + ".refresh");
            if (!project.FileExists(refreshFilePath))
            {
                string projectPath = PathUtility.EnsureTrailingSlash(project.GetFullPath("."));

                // this is the full path to the assembly file (under 'packages' folder) being referenced
                string assemblyPath = Path.Combine(packageInstallPath, assemblyReference.Path);
                // convert to relative path that is relative to the project
                string relativeAssemblyPath = PathUtility.GetRelativePath(projectPath, assemblyPath);

                try
                {
                    project.AddFile(refreshFilePath, relativeAssemblyPath.AsStream());
                }
                catch (UnauthorizedAccessException exception)
                {
                    // log IO permission error
                    ExceptionHelper.WriteToActivityLog(exception);
                }
            }
        }

        private FrameworkName GetTargetFramework(Project project)
        {
            return project.GetTargetFrameworkName() ?? VersionUtility.DefaultTargetFramework;
        }

        private class FileAssemblyReference : IPackageAssemblyReference
        {
            private readonly string _path;
            private readonly string _fullPath;
            private FrameworkName _targetFramework;

            public FileAssemblyReference(string assemblyPath, IFileSystem fileSystem)
            {
                Debug.Assert(assemblyPath.StartsWith(Constants.LibDirectory, StringComparison.OrdinalIgnoreCase));

                _path = assemblyPath;
                _fullPath = fileSystem.GetFullPath(assemblyPath);

                string pathExcludeLib = assemblyPath.Substring(Constants.LibDirectory.Length).Trim(System.IO.Path.DirectorySeparatorChar);
                _targetFramework = VersionUtility.ParseFrameworkFolderName(pathExcludeLib);
            }

            public FrameworkName TargetFramework
            {
                get { return _targetFramework; }
            }

            public string Name
            {
                get { return System.IO.Path.GetFileName(Path); }
            }

            public string Path
            {
                get { return _path; }
            }

            public Stream GetStream()
            {
                return File.OpenRead(_fullPath);
            }

            public IEnumerable<FrameworkName> SupportedFrameworks
            {
                get
                {
                    if (_targetFramework != null)
                    {
                        return new FrameworkName[] { _targetFramework };
                    }

                    return new FrameworkName[0];
                }
            }
        }
    }
}