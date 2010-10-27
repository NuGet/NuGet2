namespace NuGet {
    using System;
    using System.IO;

    public class DefaultPackagePathResolver : IPackagePathResolver {
        private readonly IFileSystem _fileSystem;
        
        public DefaultPackagePathResolver(string path)
            : this(new FileBasedProjectSystem(path)) {
        }

        public DefaultPackagePathResolver(IFileSystem fileSystem) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
        }

        public string GetInstallPath(IPackage package) {
            return Path.Combine(_fileSystem.Root, GetPackageDirectory(package));
        }

        public string GetPackageDirectory(IPackage package) {
            return package.Id + "." + package.Version;
        }

        public string GetPackageFileName(IPackage package) {
            return package.Id + "." + package.Version + Constants.PackageExtension;
        }
    }
}
