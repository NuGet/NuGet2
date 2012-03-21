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
                    let fileName = Path.GetFileNameWithoutExtension(file)
                    where FileSystem.DirectoryExists(fileName)
                    select new UnzippedPackage(FileSystem, fileName)).AsQueryable();
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            string directoryName = packageId + "." + version.ToString(); 
            string fileName = directoryName + Constants.PackageExtension;
            if (FileSystem.FileExists(fileName) && FileSystem.DirectoryExists(directoryName))
            {
                return new UnzippedPackage(FileSystem, directoryName);
            }

            return null;
        }
    }
}