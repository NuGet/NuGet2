using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet {
    /// <summary>
    /// This repository implementation keeps track of packages that are referenced in a project but
    /// it also has a reference to the repository that actually contains the packages. It keeps track
    /// of packages in an xml file at the project root (packages.xml).
    /// </summary>
    public class PackageReferenceRepository : PackageRepositoryBase, IPackageLookup {
        public static readonly string PackageReferenceFile = "packages.config";
        private readonly PackageReferenceFile _packageReferenceFile;
        private readonly string _fullPath;

        public PackageReferenceRepository(IFileSystem fileSystem, ISharedPackageRepository sourceRepository) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            if (sourceRepository == null) {
                throw new ArgumentNullException("sourceRepository");
            }
            _packageReferenceFile = new PackageReferenceFile(fileSystem, PackageReferenceFile);
            _fullPath = fileSystem.GetFullPath(PackageReferenceFile);
            SourceRepository = sourceRepository;
        }

        public override string Source {
            get {
                return PackageReferenceFile;
            }
        }

        private ISharedPackageRepository SourceRepository {
            get;
            set;
        }

        private string PackageReferenceFileFullPath {
            get {
                return _fullPath;
            }
        }

        public override IQueryable<IPackage> GetPackages() {
            return GetPackagesCore().AsSafeQueryable();
        }

        private IEnumerable<IPackage> GetPackagesCore() {
            foreach (var reference in _packageReferenceFile.GetPackageReferences()) {                
                IPackage package = null;

                if (String.IsNullOrEmpty(reference.Id) ||
                    reference.Version == null ||
                    !SourceRepository.TryFindPackage(reference.Id, reference.Version, out package)) {
                    // Skip bad entries
                    continue;
                }
                else {
                    yield return package;
                }
            }
        }

        public override void AddPackage(IPackage package) {            
            _packageReferenceFile.AddEntry(package.Id, package.Version);

            // Notify the source repository every time we add a new package to the repository.
            // This doesn't really need to happen on every package add, but this is over agressive
            // to combat scenarios where the 2 repositories get out of sync. If this repository is already 
            // registered in the source then this will be ignored
            SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
        }
 
        public override void RemovePackage(IPackage package) {            
            if (_packageReferenceFile.DeleteEntry(package.Id, package.Version)) {
                // Remove the repository from the source
                SourceRepository.UnregisterRepository(PackageReferenceFileFullPath);
            }
        }

        public IPackage FindPackage(string packageId, Version version) {
            if (!_packageReferenceFile.EntryExists(packageId, version)) {
                return null;
            }

            return SourceRepository.FindPackage(packageId, version);
        }

        public void RegisterIfNecessary() {
            if (GetPackages().Any()) {
                SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
            }
        }
    }
}