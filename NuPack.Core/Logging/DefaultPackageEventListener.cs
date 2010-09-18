namespace NuPack {
    public class DefaultPackageEventListener : IPackageEventListener {
        internal static readonly IPackageEventListener Instance = new DefaultPackageEventListener();

        public void OnBeforeInstall(OperationContext context) {
            
        }

        public void OnAfterInstall(OperationContext context) {
            
        }

        public void OnBeforeUninstall(OperationContext context) {
            
        }

        public void OnAfterUninstall(OperationContext context) {
            
        }

        public void OnReportProgress(int percent) {
            
        }

        public void OnReportStatus(StatusLevel level, string message, params object[] args) {
            
        }
    }
}
