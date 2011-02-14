namespace NuGet {
    using System;
    using System.IO;

    public class DefaultPackagePathResolver : IPackagePathResolver {
        private readonly IFileSystem _fileSystem;
        private readonly bool _useSideBySidePaths;
        
        public DefaultPackagePathResolver(string path)
            : this(new PhysicalFileSystem(path)) {
        }

        public DefaultPackagePathResolver(IFileSystem fileSystem)
            : this(fileSystem, useSideBySidePaths: true) {
        }

        public DefaultPackagePathResolver(string path, bool useSideBySidePaths)
            : this(new PhysicalFileSystem(path), useSideBySidePaths) {
        }

        public DefaultPackagePathResolver(IFileSystem fileSystem, bool useSideBySidePaths) {
            _useSideBySidePaths = useSideBySidePaths;
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
        }

        public virtual string GetInstallPath(IPackage package) {
            return Path.Combine(_fileSystem.Root, GetPackageDirectory(package));
        }

        public virtual string GetPackageDirectory(IPackage package) {
            string directory = package.Id;
            if (_useSideBySidePaths) {
                directory += "." + package.Version;
            }
            return directory;
        }

        public virtual string GetPackageFileName(IPackage package) {
            string fileNameBase = package.Id;
            if (_useSideBySidePaths) {
                fileNameBase += "." + package.Version;
            }
            return fileNameBase + Constants.PackageExtension;
        }
    }
}
