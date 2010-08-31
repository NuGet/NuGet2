
namespace NuPack {
    public class PackageEventListener {

        internal static readonly PackageEventListener Default = new PackageEventListener();

        internal PackageEventListener() {
        }

        // not called if the package is already installed
        public virtual void OnBeforeInstall(OperationContext context) {
        }

        // not called if the package is already installed
        public virtual void OnAfterInstall(OperationContext context) {
        }

        public virtual void OnBeforeUninstall(OperationContext context) {
        }

        public virtual void OnAfterUninstall(OperationContext context) {
        }

        public virtual void OnReportProgress(int percent) {
        }

        public virtual void OnReportStatus(StatusLevel level, string progressText, params object[] args) {
        }
    }
}