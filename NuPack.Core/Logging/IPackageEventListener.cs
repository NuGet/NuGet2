namespace NuPack {
    public interface IPackageEventListener {        
        // Not called if the package is already installed
        void OnBeforeInstall(OperationContext context);
        // Not called if the package is already installed
        void OnAfterInstall(OperationContext context);
        void OnBeforeUninstall(OperationContext context);
        void OnAfterUninstall(OperationContext context);
        void OnReportProgress(int percent);
        void OnReportStatus(StatusLevel level, string message, params object[] args);
    }
}