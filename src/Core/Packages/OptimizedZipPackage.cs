using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    /// <summary>
    /// Represents a NuGet package backed by a .nupkg file on disk.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ZipPackage"/>, OptimizedZipPackage doesn't store content files in memory. 
    /// Instead, it unzips the .nupkg file to a temp folder on disk, which helps reduce overall memory usage.
    /// </remarks>
    public class OptimizedZipPackage : LocalPackage
    {
        private Dictionary<string, PhysicalPackageFile> _files;
        private ICollection<IPackageAssemblyReference> _assemblyReferences;
        private ICollection<FrameworkName> _supportedFrameworks;
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystem _expandedFileSystem;
        private readonly string _packagePath;
        private string _expandedFolderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedZipPackage" /> class.
        /// </summary>
        /// <param name="fullPackagePath">The full package path on disk.</param>
        /// <exception cref="System.ArgumentException">fullPackagePath</exception>
        public OptimizedZipPackage(string fullPackagePath)
        {
            if (String.IsNullOrEmpty(fullPackagePath))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "fullPackagePath");
            }

            if (!File.Exists(fullPackagePath))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.FileDoesNotExit, fullPackagePath), 
                    "fullPackagePath");
            }

            string directory = Path.GetDirectoryName(fullPackagePath);
            _fileSystem = new PhysicalFileSystem(directory);
            _packagePath = Path.GetFileName(fullPackagePath);
            _expandedFileSystem = new PhysicalFileSystem(Path.GetTempPath());

            EnsureManifest();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedZipPackage" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system which contains the .nupkg file.</param>
        /// <param name="packagePath">The relative package path within the file system.</param>
        public OptimizedZipPackage(IFileSystem fileSystem, string packagePath)
            : this(fileSystem, packagePath, new PhysicalFileSystem(Path.GetTempPath()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedZipPackage" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system which contains the .nupkg file.</param>
        /// <param name="packagePath">The relative package path within the file system.</param>
        /// <param name="expandedFileSystem">The file system which should be used to store unzipped content files.</param>
        /// <exception cref="System.ArgumentNullException">fileSystem</exception>
        /// <exception cref="System.ArgumentException">packagePath</exception>
        public OptimizedZipPackage(IFileSystem fileSystem, string packagePath, IFileSystem expandedFileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            if (expandedFileSystem == null)
            {
                throw new ArgumentNullException("expandedFileSystem");
            }

            if (String.IsNullOrEmpty(packagePath))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packagePath");
            }

            _fileSystem = fileSystem;
            _packagePath = packagePath;
            _expandedFileSystem = expandedFileSystem;

            EnsureManifest();
        }

        public bool IsValid
        {
            get
            {
                return _fileSystem.FileExists(_packagePath);
            }
        }

        protected IFileSystem FileSystem
        {
            get
            {
                return _fileSystem;
            }
        }

        public override Stream GetStream()
        {
            return _fileSystem.OpenFile(_packagePath);
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            EnsurePackageFiles();
            return _files.Values;
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesBase()
        {
            EnsurePackageFiles();

            if (_assemblyReferences == null)
            {
                var references = from file in _files.Values
                                 where IsAssemblyReference(file)
                                 select (IPackageAssemblyReference)new PhysicalPackageAssemblyReference(file);

                _assemblyReferences = new ReadOnlyCollection<IPackageAssemblyReference>(references.ToList());
            }

            return _assemblyReferences;
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            EnsurePackageFiles();

            if (_supportedFrameworks == null)
            {
                var fileFrameworks = _files.Values.Select(c => c.TargetFramework);
                var combinedFrameworks = base.GetSupportedFrameworks()
                                             .Concat(fileFrameworks)
                                             .Where(f => f != null)
                                             .Distinct();

                _supportedFrameworks = new ReadOnlyCollection<FrameworkName>(combinedFrameworks.ToList());
            }

            return _supportedFrameworks;
        }

        private void EnsureManifest()
        {
            using (Stream stream = _fileSystem.OpenFile(_packagePath))
            {
                Package package = Package.Open(stream);
                PackageRelationship relationshipType = package.GetRelationshipsByType(Constants.PackageRelationshipNamespace + PackageBuilder.ManifestRelationType).SingleOrDefault();

                if (relationshipType == null)
                {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                PackagePart manifestPart = package.GetPart(relationshipType.TargetUri);

                if (manifestPart == null)
                {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                using (Stream manifestStream = manifestPart.GetStream())
                {
                    ReadManifest(manifestStream);
                }
            }
        }

        private void EnsurePackageFiles()
        {
            if (_files != null && _expandedFileSystem.DirectoryExists(_expandedFolderPath))
            {
                return;
            }

            _files = new Dictionary<string, PhysicalPackageFile>();
            _assemblyReferences = null;
            _supportedFrameworks = null;

            using (Stream stream = GetStream())
            {
                Package package = Package.Open(stream);

                _expandedFolderPath = GetExpandedFolderPath();

                // unzip files inside package
                var files = from part in package.GetParts()
                            where ZipPackage.IsPackageFile(part)
                            select part;

                // now copy all package's files to disk
                foreach (PackagePart file in files)
                {
                    string path = UriUtility.GetPath(file.Uri);
                    string filePath = Path.Combine(_expandedFolderPath, path);
                    
                    using (Stream partStream = file.GetStream())
                    {
                        // only copy the package part to disk if there doesn't exist
                        // a file with the same name.
                        if (!_expandedFileSystem.FileExists(filePath))
                        {
                            using (Stream targetStream = _expandedFileSystem.CreateFile(filePath))
                            {
                                partStream.CopyTo(targetStream);
                            }
                        }
                    }

                    var packageFile = new PhysicalPackageFile
                    {
                        SourcePath = _expandedFileSystem.GetFullPath(filePath),
                        TargetPath = path
                    };

                    _files[path] = packageFile;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        protected virtual string GetExpandedFolderPath()
        {
            return Path.Combine("nuget", Path.GetRandomFileName());
        }
    }
}