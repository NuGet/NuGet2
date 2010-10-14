using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuPack.Authoring;

namespace NuPack.MSBuild {
    public class NuPack : Task {
        static readonly string[] _fileExtensionsToIgnore = new[] { 
            Constants.ManifestExtension, 
            Constants.PackageExtension };
        readonly IExtendedFileSystem _fileSystem;
        readonly IPackageBuilderFactory _packageBuilderFactory;

        public NuPack()
            : this(null, null) { }

        public NuPack(
            IExtendedFileSystem fileSystem,
            IPackageBuilderFactory packageBuilderFactory) {
            _fileSystem = fileSystem ?? new FileSystemWrapper();
            _packageBuilderFactory = packageBuilderFactory ?? new PackageBuilderFactory();
        }

        public string PackageDir { get; set; }

        [Required]
        public string SpecFile { get; set; }

        public override bool Execute() {
            if (string.IsNullOrWhiteSpace(SpecFile)) {
                Log.LogError(Resources.NuPackResources.SpecFileMustNotBeEmpty);
                return false;
            }

            if (!_fileSystem.FileExists(SpecFile)) {
                Log.LogError(Resources.NuPackResources.SpecFileDoesNotExist);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PackageDir) && !_fileSystem.DirectoryExists(PackageDir)) {
                Log.LogError(Resources.NuPackResources.PackageDirDoesNotExist);
                return false;
            }

            string workingDir = _fileSystem.GetCurrentDirectory();
            string packageDir = PackageDir;
            if (packageDir == null || string.IsNullOrWhiteSpace(packageDir))
                packageDir = workingDir;

            string specFilePath = Path.Combine(workingDir, SpecFile);

            try {
                IPackageBuilder packageBuilder = _packageBuilderFactory.CreateFrom(specFilePath);
                packageBuilder.Created = DateTime.Now;
                packageBuilder.Modified = packageBuilder.Created;
                packageBuilder.Files.RemoveAll(file => _fileExtensionsToIgnore.Contains(Path.GetExtension(file.Path)));

                string packageFile = string.Format(
                    "{0}.{1}{2}",
                    packageBuilder.Id,
                    packageBuilder.Version,
                    Constants.PackageExtension);
                string packageFilePath = Path.Combine(packageDir, packageFile);

                Log.LogMessage(string.Format(
                    Resources.NuPackResources.CreatingPackage,
                    _fileSystem.GetFullPath(specFilePath),
                    _fileSystem.GetFullPath(packageFilePath)));
                
                using (Stream stream = _fileSystem.CreateFile(packageFilePath))
                    packageBuilder.Save(stream);
                
                Log.LogMessage(string.Format(
                    Resources.NuPackResources.CreatedPackage,
                    _fileSystem.GetFullPath(specFilePath),
                    _fileSystem.GetFullPath(packageFilePath)));
            }
            catch (Exception ex) {
                Log.LogError(Resources.NuPackResources.UnexpectedError, ex.ToString());
                return false;
            }

            return true;
        }
    }
}
