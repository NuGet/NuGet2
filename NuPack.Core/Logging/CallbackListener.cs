namespace NuPack {
    using System;
    using System.Globalization;
    using System.Collections;
    using Microsoft.Internal.Web.Utils;
    using System.Collections.Generic;

    public sealed class CallbackListener : PackageEventListener {

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

        public override void OnBeforeInstall(OperationContext context) {
            if (_beforeInstall != null) {
                _beforeInstall(context);
            }

        }

        public override void OnAfterInstall(OperationContext context) {
            if (_afterInstall != null) {
                _afterInstall(context);
            }
        }

        public override void OnBeforeUninstall(OperationContext context) {
            if (_beforeUninstall != null) {
                _beforeUninstall(context);
            }
        }

        public override void OnAfterUninstall(OperationContext context) {
            if (_afterUninstall != null) {
                _afterUninstall(context);
            }
        }

        public override void OnReportProgress(int percent) {
            if (_reportProgress != null) {
                _reportProgress(percent);
            }
        }

        public override void OnReportStatus(StatusLevel level, string progressText, params object[] args) {
            if (_reportStatus != null) {
                _reportStatus(level, String.Format(CultureInfo.CurrentCulture, progressText, args));
            }
        }
    }
}