using System.ComponentModel;

namespace NuGet
{
    public class PackageOperationEventArgs : CancelEventArgs
    {
        public PackageOperationEventArgs(IPackage package, IFileSystem fileSystem, string targetPath)
            : this(package, fileSystem, targetPath, targetPath)
        {
        }

        public PackageOperationEventArgs(IPackage package, IFileSystem fileSystem, string targetPath, string installPath)
        {
            Package = package;
            TargetPath = targetPath;
            InstallPath = installPath;
            FileSystem = fileSystem;
        }

        public string TargetPath { get; private set; }
        public string InstallPath { get; private set; }
        public IPackage Package { get; private set; }
        public IFileSystem FileSystem { get; private set; }
    }
}
