using System;

namespace NuGet.VisualStudio {
    public static class ExceptionHelper {
        public static void WriteToActivityLog(Exception exception) {
            IVsActivityLogger logger = ServiceLocator.GetInstance<IVsActivityLogger>();
            if (logger != null) {
                logger.LogEntry(ActivityLogEntryType.Error, exception.Message + exception.StackTrace);
            }
        }
    }
}
