using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Authoring;

namespace NuGet.MSBuild {
    public class NuGet : Task {
        static readonly string[] _fileExtensionsToIgnore = new[] { Constants.ManifestExtension, Constants.PackageExtension };
        private readonly IFileSystemProvider _fileSystemProvider;
        private readonly IPackageBuilderFactory _packageBuilderFactory;
        private readonly string _workingDirectory;

        public NuGet()
            : this(new FileSystemProvider(), new PackageBuilderFactory(), Directory.GetCurrentDirectory()) {
        }

        public NuGet(IFileSystemProvider fileSystemProvider, IPackageBuilderFactory packageBuilderFactory, string workingDirectory) {
            _fileSystemProvider = fileSystemProvider;
            _packageBuilderFactory = packageBuilderFactory;
            _workingDirectory = workingDirectory;
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

            var fileSystem = _fileSystemProvider.CreateFileSystem(_workingDirectory);

            if (!fileSystem.FileExists(SpecFile)) {
                Log.LogError(Resources.NuGetResources.SpecFileDoesNotExist);
                return false;
            }

            if (!String.IsNullOrEmpty(PackageDir) && !fileSystem.DirectoryExists(PackageDir)) {
                Log.LogError(Resources.NuGetResources.PackageDirDoesNotExist);
                return false;
            }

            string packageDir = PackageDir;

            if (String.IsNullOrEmpty(packageDir)) {
                packageDir = _workingDirectory;
            }

            string specFilePath = Path.Combine(_workingDirectory, SpecFile);

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
                    fileSystem.GetFullPath(specFilePath),
                    fileSystem.GetFullPath(packageFilePath)));

                using (var stream = new MemoryStream()) {
                    packageBuilder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    fileSystem.AddFile(packageFilePath, stream);
                }

                OutputPackage = packageFilePath;

                Log.LogMessage(String.Format(
                    Resources.NuGetResources.CreatedPackage,
                    fileSystem.GetFullPath(specFilePath),
                    fileSystem.GetFullPath(packageFilePath)));
            }
            catch (Exception ex) {
                Log.LogError(Resources.NuGetResources.UnexpectedError, ex.ToString());
                return false;
            }

            return true;
        }
    }
}
