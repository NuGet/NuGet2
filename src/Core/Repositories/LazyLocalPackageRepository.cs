using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// A physical file-system based repository that defers detecting if the directory is a v2 or a v3 style repository until
    /// the first repository operation is performed.
    /// </summary>
    public class LazyLocalPackageRepository : PackageRepositoryBase, IPackageLookup
    {
        private readonly Lazy<IPackageRepository> _repository;
        private readonly IFileSystem _fileSystem;

        public LazyLocalPackageRepository(string path)
            : this(new PhysicalFileSystem(path))
        {
        }

        public LazyLocalPackageRepository(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _repository = new Lazy<IPackageRepository>(() => CreateRepository(fileSystem));
        }

        public override string Source
        {
            get { return _fileSystem.Root; }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        // Internal for unit testing.
        internal IPackageRepository Repository
        {
            get { return _repository.Value; }
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return Repository.GetPackages();
        }

        public override void AddPackage(IPackage package)
        {
            Repository.AddPackage(package);
        }

        public override void RemovePackage(IPackage package)
        {
            Repository.RemovePackage(package);
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            return Repository.Exists(packageId, version);
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return Repository.FindPackage(packageId, version);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            return Repository.FindPackagesById(packageId);
        }

        private static IPackageRepository CreateRepository(IFileSystem fileSystem)
        {
            if (!fileSystem.DirectoryExists(path: string.Empty) ||
                fileSystem.GetFiles(path: string.Empty, filter: "*.nupkg").Any())
            {
                // If the repository does not exist or if there are .nupkg in the path, this is a v2-style repository.
                return new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);
            }

            foreach (var idDirectory in fileSystem.GetDirectories(path: string.Empty))
            {
                if (fileSystem.GetFiles(idDirectory, "*.nupkg").Any() ||
                    fileSystem.GetFiles(idDirectory, "*.nuspec").Any())
                {
                    // ~/Foo/Foo.1.0.0.nupkg (LocalPackageRepository with PackageSaveModes.Nupkg) or 
                    // ~/Foo/Foo.1.0.0.nuspec (LocalPackageRepository with PackageSaveMode.Nuspec)
                    return new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);
                }

                foreach (var versionDirectoryPath in fileSystem.GetDirectories(idDirectory))
                {
                    if (fileSystem.GetFiles(versionDirectoryPath, idDirectory + Constants.ManifestExtension).Any())
                    {
                        // If we have files in the format {packageId}/{version}/{packageId}.nuspec, assume it's an expanded package repository.
                        return new ExpandedPackageRepository(fileSystem);
                    }
                }
            }

            return new LocalPackageRepository(new DefaultPackagePathResolver(fileSystem), fileSystem);
        }
    }
}
