using System.Collections.Generic;

namespace NuGet.VisualStudio
{
    internal class VsPackageMetadata : IVsPackageMetadata
    {
        private readonly IPackage _package;
        private readonly string _installPath;
        private readonly IFileSystem _fileSystem;

        public VsPackageMetadata(IPackage package, string installPath) :
            this(package, installPath, fileSystem: null)
        {
        }

        public VsPackageMetadata(IPackage package, string installPath, IFileSystem fileSystem)
        {
            _package = package;
            _installPath = installPath;
            _fileSystem = fileSystem;
        }

        public string Id
        {
            get { return _package.Id; }
        }

        public SemanticVersion Version
        {
            get { return _package.Version; }
        }

        public string VersionString
        {
            get { return _package.Version.ToString(); }
        }

        public string Title
        {
            get { return _package.Title; }
        }

        public IEnumerable<string> Authors
        {
            get { return _package.Authors; }
        }

        public string Description
        {
            get { return _package.Description; }
        }

        public string InstallPath
        {
            get { return _installPath; }
        }

        public IFileSystem FileSystem
        {
            get { return _fileSystem; }
        }
    }
}
