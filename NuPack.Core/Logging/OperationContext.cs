
namespace NuPack {

    public class OperationContext {

        public OperationContext(IPackage package, string targetPath) {
            Package = package;
            TargetPath = targetPath;
        }

        public string TargetPath { get; private set; }

        public IPackage Package { get; private set; }
    }
}