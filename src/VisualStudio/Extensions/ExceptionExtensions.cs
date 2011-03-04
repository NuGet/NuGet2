using System;

namespace NuGet.VisualStudio {
    public static class ExceptionExtensions {

        public static void WriteToActivityLog(this Exception exception) {
            IVsActivityLogger logger = ServiceLocator.GetInstance<IVsActivityLogger>();
            if (logger != null) {
                logger.LogEntry(ActivityLogEntryType.Error, exception.Message + exception.StackTrace);
            }
        }
    }
}
