namespace NuPack {
    public class OperationContext {
        public OperationContext(IPackage package, string targetPath)
            : this(package, targetPath, targetPath) {
        }

        public OperationContext(IPackage package, string targetPath, string installPath) {
            Package = package;
            TargetPath = targetPath;
            InstallPath = installPath;
        }

        public string TargetPath { get; private set; }
        public string InstallPath { get; private set; }
        public IPackage Package { get; private set; }
    }
}