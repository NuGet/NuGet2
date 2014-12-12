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
                        // Get the absolute path to the assembly being added. 
                        string assemblyPath = Path.Combine(packageDirectory, assemblyReference.Path);

                        // create one refresh file for each assembly reference, as per required by Website projects
                        projectSystem.CreateRefreshFile(assemblyPath);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the native binaries to the project's bin folder.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="packagesFileSystem">The packages file system.</param>
        /// <param name="packageNames">The package names.</param>
        public void CopyNativeBinaries(Project project, IFileSystem packagesFileSystem, IEnumerable<PackageName> packageNames)
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

            IProjectSystem projectSystem = _projectSystemFactory != null ? _projectSystemFactory.CreateProjectSystem(project, _fileSystemProvider)
                                                                          : VsProjectSystemFactory.CreateProjectSystem(project, _fileSystemProvider);

            foreach (PackageName packageName in packageNames)
            {
                CopyNativeBinaries(projectSystem, packagesFileSystem, packageName);
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
                    // TODO: SearchFilesWithinOneSubFolders seems fragile. In the event conventions in the lib directory change to allow more than one level of nesting, it would 
                    // not work. We should let VS perform a regular install instead of doing this. 
                    return Constants.AssemblyReferencesExtensions
                                    .Select(extension => "*" + extension)
                                    .SelectMany(extension => SearchFilesWithinOneSubFolders(fileSystem, libFolderPath, extension))
                                    .Select(assembly => new FileAssemblyReference(assembly.Substring(packageName.Length).Trim(Path.DirectorySeparatorChar)));
                }
            }

            packageDirectory = null;
            return Enumerable.Empty<IPackageAssemblyReference>();
        }

        private IEnumerable<string> SearchFilesWithinOneSubFolders(IFileSystem fileSystem, string folder, string extension)
        {
            // get files directly under 'folder'
            var filesUnderFolder = fileSystem
                .GetFiles(folder, extension);

            // get files under subfolders of 'folder'
            var fileUnderSubFolder = fileSystem.GetDirectories(folder)
                .SelectMany(subFolder => fileSystem.GetFiles(subFolder, extension));

            return filesUnderFolder.Concat(fileUnderSubFolder);
        }

        private void CopyNativeBinaries(IProjectSystem projectSystem, IFileSystem packagesFileSystem, PackageName packageName)
        {
            const string nativeBinariesFolder = "NativeBinaries";

            string nativeBinariesPath = Path.Combine(packageName.Name, nativeBinariesFolder);
            if (packagesFileSystem.DirectoryExists(nativeBinariesPath))
            {
                IEnumerable<string> nativeFiles = packagesFileSystem.GetFiles(nativeBinariesPath, "*.*", recursive: true);
                foreach (string file in nativeFiles)
                {
                    string targetPath = Path.Combine(Constants.BinDirectory, file.Substring(nativeBinariesPath.Length + 1));  // skip over NativeBinaries/ word
                    using (Stream stream = packagesFileSystem.OpenFile(file)) 
                    {
                        projectSystem.AddFile(targetPath, stream);
                    }
                }
            }
        }

        private FrameworkName GetTargetFramework(Project project)
        {
            return project.GetTargetFrameworkName() ?? VersionUtility.DefaultTargetFramework;
        }

        private sealed class FileAssemblyReference : IPackageAssemblyReference
        {
            private readonly string _path;
            private readonly string _effectivePath;
            private readonly FrameworkName _targetFramework;

            public FileAssemblyReference(string assemblyPath)
            {
                Debug.Assert(assemblyPath.StartsWith(Constants.LibDirectory, StringComparison.OrdinalIgnoreCase));

                _path = assemblyPath;

                string pathExcludeLib = assemblyPath.Substring(Constants.LibDirectory.Length).Trim(System.IO.Path.DirectorySeparatorChar);
                _targetFramework = VersionUtility.ParseFrameworkFolderName(pathExcludeLib, strictParsing: true, effectivePath: out _effectivePath);
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

            public string EffectivePath
            {
                get
                {
                    return _effectivePath;
                }
            }

            public Stream GetStream()
            {
                throw new NotSupportedException();
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