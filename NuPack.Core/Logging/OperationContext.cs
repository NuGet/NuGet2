
namespace NuPack {

    public class OperationContext {

        public OperationContext(Package package, string targetPath) {
            Package = package;
            TargetPath = targetPath;
        }

        public string TargetPath { get; private set; }

        public Package Package { get; private set; }
    }
}