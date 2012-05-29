using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// A helper manager that allows keeping track of satellite packages that were installed alongside a core package.
    /// </summary>
    public class SatellitePackageManager
    {
        private const string SatelliteReferenceDirectory = ".satellite";
        private const string SatelliteReferenceExtension = ".ref";
        private const char IdVersionSeparator = '$';
        private readonly IPackageRepository _localRepository;
        private readonly IFileSystem _fileSystem;
        private readonly IPackagePathResolver _pathResolver;

        public SatellitePackageManager(IPackageRepository localRepository, IFileSystem fileSystem, IPackagePathResolver pathResolver)
        {
            if (localRepository == null)
            {
                throw new ArgumentNullException("localRepository");
            }
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (pathResolver == null)
            {
                throw new ArgumentNullException("pathResolver");
            }

            _localRepository = localRepository;
            _fileSystem = fileSystem;
            _pathResolver = pathResolver;
        }

        /// <summary>
        /// Copies satellite files to dependent runtime package if the current package is a satellite package and the corresponding runtime package exists.
        /// </summary>
        public bool ExpandSatellitePackage(IPackage package)
        {
            IEnumerable<IPackage> runtimePackages;
            if (IsSatellitePackage(package, out runtimePackages))
            {
                foreach (var runtimePackage in runtimePackages)
                {
                    var satelliteFiles = package.GetSatelliteFiles();
                    var runtimePath = _pathResolver.GetPackageDirectory(runtimePackage);
                    _fileSystem.AddFiles(satelliteFiles, runtimePath);
                    AddSatellitePackageReference(runtimePath, package);
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Copies satellite files for one or more satellite packages to the corresponding dependent package.
        /// </summary>
        /// <param name="satellitePackages"></param>
        public void ExpandSatellitePackages(IEnumerable<IPackage> satellitePackages)
        {
            foreach (var satellitePackage in satellitePackages)
            {
                ExpandSatellitePackage(satellitePackage);
            }
        }

        /// <summary>
        /// If the package is a satellite package, removes files and references from the corresponding runtime package.
        /// If the package has satellite references, removes these from the package's install directory.
        /// </summary>
        /// <returns>true if any satellite files were removed, false otherwise.</returns>
        public bool RemoveSatelliteReferences(IPackage package)
        {
            // If this is a Satellite Package, then remove the files from the related runtime package folder too
            IEnumerable<IPackage> runtimePackages;
            if (IsSatellitePackage(package, out runtimePackages))
            {
                RemoveSatellitePackageInternal(runtimePackages, package);
                return true;
            }
            else
            {
                // If the package has any satellite references, remove them from the directory.
                var satelliteReferences = GetSatelliteReferences(package);
                if (satelliteReferences.Any())
                {
                    foreach (var satellitePackage in satelliteReferences)
                    {
                        RemoveSatellitePackageInternal(new[] { package }, satellitePackage);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets satellite packages for the current package that are installed into the local repository.
        /// </summary>
        public IEnumerable<IPackage> GetSatelliteReferences(IPackage package)
        {
            var packageDirectory = _pathResolver.GetPackageDirectory(package);
            var satelliteDirectory = Path.Combine(packageDirectory, SatelliteReferenceDirectory);
            return (from path in _fileSystem.GetFiles(satelliteDirectory, "*" + SatelliteReferenceExtension)
                    let satellitePackage = GetSatellitePackage(path)
                    where satellitePackage != null
                    select satellitePackage).ToArray();
        }

        /// <summary>
        /// Determines if a package is a satellite package and if so, returns the corresponding runtime package.
        /// </summary>
        /// <remarks>TODO: We only need to identify the id and version of the corresponding runtime package. We might be able to get away with not </remarks>
        internal bool IsSatellitePackage(IPackage package, out IEnumerable<IPackage> runtimePackages)
        {
            // A satellite package has the following properties:
            //     1) A package suffix that matches the package's language, with a dot preceding it
            //     2) A dependency on the package with the same Id minus the language suffix
            //     3) The dependency can be found by Id in the repository (as its path is needed for installation)
            // Example: foo.ja-jp, with a dependency on foo

            runtimePackages = null;
            if (!String.IsNullOrEmpty(package.Language) && package.Id.EndsWith("." + package.Language, StringComparison.OrdinalIgnoreCase))
            {
                string runtimePackageId = package.Id.Substring(0, package.Id.Length - (package.Language.Length + 1));
                PackageDependency dependency = package.FindDependency(runtimePackageId, targetFramework: null);

                if (dependency != null)
                {
                    runtimePackages = _localRepository.FindPackages(runtimePackageId, dependency.VersionSpec, allowPrereleaseVersions: true, allowUnlisted: true).ToList();
                }
            }

            return !runtimePackages.IsEmpty();
        }

        private void RemoveSatellitePackageInternal(IEnumerable<IPackage> runtimePackages, IPackage satellitePackage)
        {
            var satelliteFiles = satellitePackage.GetSatelliteFiles();

            foreach (var runtimePackage in runtimePackages)
            {
                var runtimePath = _pathResolver.GetPackageDirectory(runtimePackage);
                _fileSystem.DeleteFiles(satelliteFiles, runtimePath);
                RemoveSatellitePackageReference(runtimePath, satellitePackage);
            }
        }

        private IPackage GetSatellitePackage(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string[] tokens = fileName.Trim().Split(new[] { IdVersionSeparator }, StringSplitOptions.RemoveEmptyEntries);

            SemanticVersion version;
            if (tokens.Length > 1 && 
                SemanticVersion.TryParse(tokens[1], out version))
            {
                // Satellite reference file names are formatted as <Id>$<Version>.ref. Ensure we can extract both those tokens from the file name
                // and that the file is parseable.
                string id = tokens[0];
                return _localRepository.FindPackage(id, version);
            }
            return null;
        }

        /// <summary>
        /// Adds a back reference to the satellite package in the core package installed at runtimePath.
        /// A back reference is a file under .satellite of the runtimePath of the format
        /// id$version.ref
        /// e.g. A sample file would look like packages\Foo.1.0\.satellite\Foo.fr-FR$1.0
        /// </summary>
        private void AddSatellitePackageReference(string runtimePath, IPackage package)
        {
            string fileName = package.Id + IdVersionSeparator + package.Version + SatelliteReferenceExtension;
            string path = Path.Combine(runtimePath, SatelliteReferenceDirectory, fileName);
            using (var stream = Stream.Null)
            {
                _fileSystem.AddFile(path, stream);
            }
        }

        /// <summary>
        /// Removes a back reference to the satellite package from the runtimePath's .satellite directory.
        /// </summary>
        private void RemoveSatellitePackageReference(string runtimePath, IPackage package)
        {
            string fileName = package.Id + IdVersionSeparator + package.Version + SatelliteReferenceExtension;
            var satelliteDirectory = Path.Combine(runtimePath, SatelliteReferenceDirectory);
            string path = Path.Combine(satelliteDirectory, fileName);
            _fileSystem.DeleteFileSafe(path);

            // If the directory is empty, delete it.
            if (!_fileSystem.GetFiles(satelliteDirectory, "*.*", recursive: true).Any())
            {
                _fileSystem.DeleteDirectorySafe(satelliteDirectory, recursive: true);
            }
        }
    }
}
