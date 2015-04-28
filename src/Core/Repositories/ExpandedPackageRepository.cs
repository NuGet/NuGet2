using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// Represents a NuGet v3 style expanded repository. Packages in this repository are 
    /// stored in the format {id}/{version}/{unzipped-contents}
    /// </summary>
    public class ExpandedPackageRepository : PackageRepositoryBase, IPackageLookup
    {
        private readonly IFileSystem _fileSystem;

        public ExpandedPackageRepository(string physicalPath)
            : this(new PhysicalFileSystem(physicalPath))
        {
        }

        public ExpandedPackageRepository(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public override string Source
        {
            get { return _fileSystem.Root; }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        public override void AddPackage(IPackage package)
        {
            var packagePath = GetPackagePath(package.Id, package.Version);
            foreach (var file in package.GetFiles())
            {
                using (var stream = file.GetStream())
                {
                    _fileSystem.AddFile(Path.Combine(packagePath, file.Path), stream);
                }
            }

            using (var stream = package.GetStream())
            {
                var nupkgPath = Path.Combine(packagePath, package.Id + "." + package.Version.ToNormalizedString() + Constants.PackageExtension);
                _fileSystem.AddFile(nupkgPath, stream);
            }

            using (var stream = package.GetStream())
            {
                using (var manifestStream = PackageHelper.GetManifestStream(stream))
                {
                    var manifestPath = Path.Combine(packagePath, package.Id + Constants.ManifestExtension);
                    _fileSystem.AddFile(manifestPath, manifestStream);
                }
            }
        }

        public override void RemovePackage(IPackage package)
        {
            if (Exists(package.Id, package.Version))
            {
                var packagePath = GetPackagePath(package.Id, package.Version);
                _fileSystem.DeleteDirectorySafe(packagePath, recursive: true);
            }
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            var packagePath = GetPackagePath(packageId, version);
            var manifestPath = Path.Combine(packagePath, packageId + Constants.ManifestExtension);
            return _fileSystem.FileExists(manifestPath);
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            var packagePath = GetPackagePath(packageId, version);
            if (_fileSystem.FileExists(Path.Combine(packagePath, packageId + Constants.ManifestExtension)))
            {
                return new UnzippedPackage(_fileSystem, packageId, version);
            }

            return null;
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            foreach (var versionDirectory in _fileSystem.GetDirectoriesSafe(packageId))
            {
                var versionDirectoryName = Path.GetFileName(versionDirectory);
                SemanticVersion version;
                if (SemanticVersion.TryParse(versionDirectoryName, out version) &&
                    _fileSystem.FileExists(Path.Combine(versionDirectory, packageId + Constants.ManifestExtension)))
                {
                    yield return new UnzippedPackage(_fileSystem, packageId, version);
                }
            }
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return _fileSystem.GetDirectoriesSafe(path: string.Empty)
                .SelectMany(packageDirectory =>
                {
                    var packageId = Path.GetFileName(packageDirectory);
                    return FindPackagesById(packageId);
                }).AsQueryable();
        }

        private static string GetPackagePath(string packageId, SemanticVersion version)
        {
            return Path.Combine(packageId, version.ToNormalizedString());
        }
    }
}
