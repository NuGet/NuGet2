using System.IO;
using System.Linq;

namespace NuGet
{
    public class UnzippedPackageRepository : PackageRepositoryBase, IPackageLookup
    {
        public UnzippedPackageRepository(string physicalPath)
            : this(new DefaultPackagePathResolver(physicalPath), new PhysicalFileSystem(physicalPath))
        {
        }

        public UnzippedPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
            PathResolver = pathResolver;
        }

        protected IFileSystem FileSystem
        {
            get;
            private set;
        }

        internal IPackagePathResolver PathResolver
        {
            get;
            set;
        }

        public override string Source
        {
            get { return FileSystem.Root; }
        }

        public override bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return (from file in FileSystem.GetFiles("", "*" + Constants.PackageExtension)
                    let packageName = Path.GetFileNameWithoutExtension(file)
                    where FileSystem.DirectoryExists(packageName)
                    select new UnzippedPackage(FileSystem, packageName)).AsQueryable();
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            string packageName = packageId + "." + version.ToString(); 
            string packageFile = packageName + Constants.PackageExtension;
            if (FileSystem.FileExists(packageFile) && FileSystem.DirectoryExists(packageName))
            {
                return new UnzippedPackage(FileSystem, packageName);
            }

            return null;
        }
    }
}