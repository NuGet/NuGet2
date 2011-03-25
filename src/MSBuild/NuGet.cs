using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Authoring;

namespace NuGet.MSBuild {
    public class NuGet : Task {
        static readonly string[] _fileExtensionsToIgnore = new[] { 
            Constants.ManifestExtension, 
            Constants.PackageExtension };
        readonly IExtendedFileSystem _fileSystem;
        readonly IPackageBuilderFactory _packageBuilderFactory;

        public NuGet()
            : this(null, null) { }

        public NuGet(
            IExtendedFileSystem fileSystem,
            IPackageBuilderFactory packageBuilderFactory) {
            _fileSystem = fileSystem ?? new FileSystemWrapper();
            _packageBuilderFactory = packageBuilderFactory ?? new PackageBuilderFactory();
        }

        public string PackageDir { get; set; }

        [Required]
        public string SpecFile { get; set; }

        [Output]
        public string OutputPackage { get; set; }

        public override bool Execute() {
            if (string.IsNullOrWhiteSpace(SpecFile)) {
                Log.LogError(Resources.NuGetResources.SpecFileMustNotBeEmpty);
                return false;
            }

            if (!_fileSystem.FileExists(SpecFile)) {
                Log.LogError(Resources.NuGetResources.SpecFileDoesNotExist);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PackageDir) && !_fileSystem.DirectoryExists(PackageDir)) {
                Log.LogError(Resources.NuGetResources.PackageDirDoesNotExist);
                return false;
            }

            string workingDir = _fileSystem.GetCurrentDirectory();
            string packageDir = PackageDir;
            if (packageDir == null || string.IsNullOrWhiteSpace(packageDir))
                packageDir = workingDir;

            string specFilePath = Path.Combine(workingDir, SpecFile);

            try {
                IPackageBuilder packageBuilder = _packageBuilderFactory.CreateFrom(specFilePath);
                packageBuilder.Files.RemoveAll(file => _fileExtensionsToIgnore.Contains(Path.GetExtension(file.Path)));

                string packageFile = String.Format(
                    "{0}.{1}{2}",
                    packageBuilder.Id,
                    packageBuilder.Version,
                    Constants.PackageExtension);
                string packageFilePath = Path.Combine(packageDir, packageFile);

                Log.LogMessage(String.Format(
                    Resources.NuGetResources.CreatingPackage,
                    _fileSystem.GetFullPath(specFilePath),
                    _fileSystem.GetFullPath(packageFilePath)));

                using (Stream stream = _fileSystem.CreateFile(packageFilePath)) {
                    packageBuilder.Save(stream);
                }

                OutputPackage = packageFilePath;

                Log.LogMessage(String.Format(
                    Resources.NuGetResources.CreatedPackage,
                    _fileSystem.GetFullPath(specFilePath),
                    _fileSystem.GetFullPath(packageFilePath)));
            }
            catch (Exception ex) {
                Log.LogError(Resources.NuGetResources.UnexpectedError, ex.ToString());
                return false;
            }

            return true;
        }
    }
}
