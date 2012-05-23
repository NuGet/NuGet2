using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    /// <summary>
    /// This repository implementation keeps track of packages that are referenced in a project but
    /// it also has a reference to the repository that actually contains the packages. It keeps track
    /// of packages in an xml file at the project root (packages.xml).
    /// </summary>
    public class PackageReferenceRepository : PackageRepositoryBase, IPackageReferenceRepository, IPackageLookup, IPackageConstraintProvider, ILatestPackageLookup
    {
        private readonly PackageReferenceFile _packageReferenceFile;
        private readonly string _fullPath;

        public PackageReferenceRepository(IFileSystem fileSystem, ISharedPackageRepository sourceRepository)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (sourceRepository == null)
            {
                throw new ArgumentNullException("sourceRepository");
            }
            _packageReferenceFile = new PackageReferenceFile(fileSystem, Constants.PackageReferenceFile);
            _fullPath = fileSystem.GetFullPath(Constants.PackageReferenceFile);
            SourceRepository = sourceRepository;
        }

        public override string Source
        {
            get
            {
                return Constants.PackageReferenceFile;
            }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        private ISharedPackageRepository SourceRepository
        {
            get;
            set;
        }

        private string PackageReferenceFileFullPath
        {
            get
            {
                return _fullPath;
            }
        }

        public PackageReferenceFile ReferenceFile
        {
            get
            {
                return _packageReferenceFile;
            }
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return GetPackagesCore().AsQueryable();
        }

        private IEnumerable<IPackage> GetPackagesCore()
        {
            foreach (var reference in _packageReferenceFile.GetPackageReferences())
            {
                IPackage package;

                if (String.IsNullOrEmpty(reference.Id) ||
                    reference.Version == null ||
                    !SourceRepository.TryFindPackage(reference.Id, reference.Version, out package))
                {

                    // Skip bad entries
                    continue;
                }
                else
                {
                    yield return package;
                }
            }
        }

        public override void AddPackage(IPackage package)
        {
            AddPackage(package.Id, package.Version, targetFramework: null);
        }

        public override void RemovePackage(IPackage package)
        {
            if (_packageReferenceFile.DeleteEntry(package.Id, package.Version))
            {
                // Remove the repository from the source
                SourceRepository.UnregisterRepository(PackageReferenceFileFullPath);
            }
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            if (!_packageReferenceFile.EntryExists(packageId, version))
            {
                return null;
            }

            return SourceRepository.FindPackage(packageId, version);
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            return _packageReferenceFile.EntryExists(packageId, version);
        }

        public void RegisterIfNecessary()
        {
            if (GetPackages().Any())
            {
                SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
            }
        }

        public IVersionSpec GetConstraint(string packageId)
        {
            // Find the reference entry for this package
            var reference = GetPackageReference(packageId);
            if (reference != null)
            {
                return reference.VersionConstraint;
            }
            return null;
        }

        private PackageReference GetPackageReference(string packageId)
        {
            PackageReference reference =
                _packageReferenceFile.GetPackageReferences().FirstOrDefault(
                    p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
            return reference;
        }

        public bool TryFindLatestPackageById(string id, out SemanticVersion latestVersion)
        {
            PackageName packageName = _packageReferenceFile.FindEntryWithLatestVersionById(id);
            if (packageName == null)
            {
                latestVersion = null;
                return false;
            }
            else
            {
                latestVersion = packageName.Version;
                Debug.Assert(latestVersion != null);
                return true;
            }
        }

        public void AddPackage(string packageId, SemanticVersion version, FrameworkName targetFramework)
        {
            _packageReferenceFile.AddEntry(packageId, version, targetFramework);

            // Notify the source repository every time we add a new package to the repository.
            // This doesn't really need to happen on every package add, but this is over agressive
            // to combat scenarios where the 2 repositories get out of sync. If this repository is already 
            // registered in the source then this will be ignored
            SourceRepository.RegisterRepository(PackageReferenceFileFullPath);
        }

        public FrameworkName GetPackageTargetFramework(string packageId)
        {
            var reference = GetPackageReference(packageId);
            if (reference != null)
            {
                return reference.TargetFramework;
            }
            return null;
        }
    }
}