namespace NuPack {
    using System;
    using System.Globalization;

    public sealed class CallbackListener : IPackageEventListener {
        private Action<OperationContext> _beforeInstall, _afterInstall, _beforeUninstall, _afterUninstall;
        private Action<StatusLevel, string> _reportStatus;
        private Action<int> _reportProgress;

        public CallbackListener(
            Action<OperationContext> beforeInstall,
            Action<OperationContext> afterInstall,
            Action<OperationContext> beforeUninstall,
            Action<OperationContext> afterUninstall,
            Action<StatusLevel, string> reportStatus,
            Action<int> reportProgress) {

            _beforeInstall = beforeInstall;
            _afterInstall = afterInstall;
            _beforeUninstall = beforeUninstall;
            _afterUninstall = afterUninstall;
            _reportProgress = reportProgress;
            _reportStatus = reportStatus;
        }

        public void OnBeforeInstall(OperationContext context) {
            if (_beforeInstall != null) {
                _beforeInstall(context);
            }

        }

        public void OnAfterInstall(OperationContext context) {
            if (_afterInstall != null) {
                _afterInstall(context);
            }
        }

        public void OnBeforeUninstall(OperationContext context) {
            if (_beforeUninstall != null) {
                _beforeUninstall(context);
            }
        }

        public void OnAfterUninstall(OperationContext context) {
            if (_afterUninstall != null) {
                _afterUninstall(context);
            }
        }

        public void OnReportProgress(int percent) {
            if (_reportProgress != null) {
                _reportProgress(percent);
            }
        }

        public void OnReportStatus(StatusLevel level, string message, params object[] args) {
            if (_reportStatus != null) {
                _reportStatus(level, String.Format(CultureInfo.CurrentCulture, message, args));
            }
        }
    }
}