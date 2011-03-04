using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio {
    [Export(typeof(IVsActivityLogger))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class VsActivityLogger : IVsActivityLogger {

        // This is the package name. No need to localize it.
        public const string LogEntrySource = "NuGet Package Manager";

        private readonly Lazy<IVsActivityLog> _activityLog;

        public VsActivityLogger() : this(ServiceLocator.GetInstance<IServiceProvider>()) {
        }

        public VsActivityLogger(IServiceProvider serviceProvider) {
            _activityLog = new Lazy<IVsActivityLog>(() => (IVsActivityLog)serviceProvider.GetService(typeof(SVsActivityLog)));
        }

        public void LogEntry(ActivityLogEntryType entryType, string description) {
            if (description == null) {
                throw new ArgumentNullException("description");
            }

            __ACTIVITYLOG_ENTRYTYPE vsEntryType = ConvertToVsEntryType(entryType);
            _activityLog.Value.LogEntry((uint)vsEntryType, LogEntrySource, description);
        }

        private __ACTIVITYLOG_ENTRYTYPE ConvertToVsEntryType(ActivityLogEntryType entryType) {
            switch (entryType) {
                case ActivityLogEntryType.Information:
                    return __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;

                case ActivityLogEntryType.Warning:
                    return __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;

                case ActivityLogEntryType.Error:
                    return __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;

                default:
                    throw new ArgumentOutOfRangeException("entryType");
            }
        }
    }
}