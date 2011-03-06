namespace NuGet.VisualStudio {
    public interface IVsActivityLogger {
        void LogEntry(ActivityLogEntryType entryType, string description);
    }
}
