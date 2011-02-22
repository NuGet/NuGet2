namespace NuGet {
    public interface IProgressReporter {
        void ReportProgress(string operation, int percentComplete);
    }
}
