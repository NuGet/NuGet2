using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Resources;

namespace NuGet
{
    /// <summary>
    /// An unzipped package has its contents laid out as physical files on disk inside a directory, 
    /// instead of inside a .nupkg file.
    /// </summary>
    /// <remarks>
    /// An unzipped package is strictly required to have this directory structure: ([] denotes directory)
    /// 
    ///    jQuery.1.0.nupkg
    ///    [jQuery.1.0]
    ///         jQuery.1.0.nuspec
    ///         [content]
    ///              jQuery.js
    ///         [lib]
    ///              jQuery.dll
    ///         [tools]
    ///              install.ps1
    /// </remarks>
    internal class UnzippedPackage : LocalPackage
    {
        private readonly IFileSystem _repositoryFileSystem;
        private readonly string _packageFileName;
        private readonly string _packagePath;

        /// <summary>
        /// Create an uninstance of UnzippedPackage class
        /// </summary>
        /// <param name="repositoryDirectory">The root directory which contains the .nupkg file and the corresponding unzipped directory.</param>
        /// <param name="packageName">Contains the file name without the extension of the nupkg file.</param>
        public UnzippedPackage(string repositoryDirectory, string packageName)
            : this(new PhysicalFileSystem(repositoryDirectory), packageName)
        {
        }

        public UnzippedPackage(IFileSystem repositoryFileSystem, string packageName)
        {
            if (repositoryFileSystem == null)
            {
                throw new ArgumentNullException("repositoryFileSystem");
            }

            if (String.IsNullOrEmpty(packageName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageName");
            }

            _packageFileName = packageName + Constants.PackageExtension;
            _packagePath = packageName;
            _repositoryFileSystem = repositoryFileSystem;

            // we look for the .nuspec file at jQuery.1.4\jQuery.1.4.nuspec
            var manifestPath = Path.Combine(packageName, packageName + Constants.ManifestExtension);
            EnsureManifest(manifestPath);
        }

        public UnzippedPackage(IFileSystem repositoryFileSystem, string packageId, SemanticVersion version)
        {
            if (repositoryFileSystem == null)
            {
                throw new ArgumentNullException("repositoryFileSystem");
            }

            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            _repositoryFileSystem = repositoryFileSystem;
            _packagePath = Path.Combine(packageId, version.ToNormalizedString());
            _packageFileName = Path.Combine(_packagePath, packageId + "." + version.ToNormalizedString() + Constants.PackageExtension);
            EnsureManifest(Path.Combine(_packagePath, packageId + Constants.ManifestExtension));
        }

        public override Stream GetStream()
        {
            // first check the .nupkg file directly under root, e.g. \A.1.0.0.nupkg
            if (_repositoryFileSystem.FileExists(_packageFileName)) 
            {
                return _repositoryFileSystem.OpenFile(_packageFileName);
            }

            // if not exists, check under \A.1.0.0\A.1.0.0.nupkg
            string path = Path.Combine(_packagePath, _packageFileName);
            return _repositoryFileSystem.OpenFile(path);
        }

        public override void ExtractContents(IFileSystem fileSystem, string extractPath)
        {
            foreach (var file in GetFilesBase().Cast<PhysicalPackageFile>())
            {
                var targetPath = Path.Combine(extractPath, file.TargetPath);
                using (var fileStream = file.GetStream())
                {
                    fileSystem.AddFile(targetPath, fileStream);
                }
            }
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            string effectivePath;
            IEnumerable<FrameworkName> fileFrameworks = from file in GetPackageFilePaths().Select(GetPackageRelativePath)
                                                        let targetFramework = VersionUtility.ParseFrameworkNameFromFilePath(file, out effectivePath)
                                                        where targetFramework != null
                                                        select targetFramework;
            return base.GetSupportedFrameworks()
                       .Concat(fileFrameworks)
                       .Distinct();
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            return from p in GetPackageFilePaths()
                   select new PhysicalPackageFile
                          {
                              SourcePath = _repositoryFileSystem.GetFullPath(p),
                              TargetPath = GetPackageRelativePath(p)
                          };
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesCore()
        {
            string libDirectory = Path.Combine(_packagePath, Constants.LibDirectory);

            return from p in _repositoryFileSystem.GetFiles(libDirectory, "*.*", recursive: true)
                   let targetPath = GetPackageRelativePath(p)
                   where IsAssemblyReference(targetPath)
                   select new PhysicalPackageAssemblyReference
                            {
                                SourcePath = _repositoryFileSystem.GetFullPath(p),
                                TargetPath = targetPath
                            };
        }

        private IEnumerable<string> GetPackageFilePaths()
        {
            return from p in _repositoryFileSystem.GetFiles(_packagePath, "*.*", recursive: true)
                   where !PackageHelper.IsManifest(p) && !PackageHelper.IsPackageFile(p)
                   select p;
        }

        private string GetPackageRelativePath(string path)
        {
            // Package paths returned by the file system contain the package name. We need to yank this out of the package name because the paths we are interested in are
            // package relative paths.
            Debug.Assert(path.StartsWith(_packagePath, StringComparison.OrdinalIgnoreCase));
            return path.Substring(_packagePath.Length + 1);
        }

        private void EnsureManifest(string manifestFilePath)
        {
            if (!_repositoryFileSystem.FileExists(manifestFilePath))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_NotFound, _repositoryFileSystem.GetFullPath(manifestFilePath)));
            }

            using (Stream manifestStream = _repositoryFileSystem.OpenFile(manifestFilePath))
            {
                ReadManifest(manifestStream);
            }

            Published = _repositoryFileSystem.GetLastModified(manifestFilePath);
        }
    }
}