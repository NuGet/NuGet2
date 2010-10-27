namespace NuGet {
    using System;
    using System.ComponentModel;

    public class PackageOperationEventArgs : CancelEventArgs {
        public PackageOperationEventArgs(IPackage package, string targetPath)
            : this(package, targetPath, targetPath) {
        }

        public PackageOperationEventArgs(IPackage package, string targetPath, string installPath) {
            Package = package;
            TargetPath = targetPath;
            InstallPath = installPath;
        }

        public string TargetPath { get; private set; }
        public string InstallPath { get; private set; }
        public IPackage Package { get; private set; }
    }
}
