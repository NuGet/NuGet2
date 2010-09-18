namespace NuPack {
    using System;
    using System.IO;

    public class DefaultPackageAssemblyPathResolver : IPackageAssemblyPathResolver {
        private readonly IFileSystem _fileSystem;
        
        public DefaultPackageAssemblyPathResolver(string path)
            : this(new FileBasedProjectSystem(path)) {
        }

        public DefaultPackageAssemblyPathResolver(IFileSystem fileSystem) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
        }

        public string GetAssemblyPath(IPackage package, IPackageAssemblyReference assemblyReference) {
            return Path.Combine(_fileSystem.Root, Utility.GetPackageDirectory(package), assemblyReference.Path);
        }
    }
}
