namespace NuGet.VisualStudio {
    public interface IProgressReporter {
        void ReportProgress(string currentOperation, int percentComplete);
    }
}
