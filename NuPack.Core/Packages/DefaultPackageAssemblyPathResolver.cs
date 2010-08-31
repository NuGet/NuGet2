namespace NuPack {
    using System;
    using System.IO;

    public class DefaultPackageAssemblyPathResolver : IPackageAssemblyPathResolver {
        private readonly IFileSystem _fileSystem;
        private readonly string _referenceDirectory;

        public DefaultPackageAssemblyPathResolver(string path, string referenceDirectory)
            : this(new FileBasedProjectSystem(path), referenceDirectory) {
        }

        public DefaultPackageAssemblyPathResolver(IFileSystem fileSystem, string referenceDirectory) {
            if (fileSystem == null) {
                throw new ArgumentNullException("fileSystem");
            }
            _fileSystem = fileSystem;
            _referenceDirectory = referenceDirectory;
        }

        public string GetAssemblyPath(Package package, IPackageAssemblyReference assemblyReference) {
            return Path.Combine(_fileSystem.Root, Utility.GetPackageDirectory(package), _referenceDirectory, assemblyReference.Path);
        }
    }
}
