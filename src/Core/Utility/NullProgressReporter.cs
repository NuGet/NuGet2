namespace NuGet {
    public sealed class NullProgressReporter : IProgressReporter {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "This type is immutable.")]
        public static readonly IProgressReporter Instance = new NullProgressReporter();

        private NullProgressReporter() {
        }

        public void ReportProgress(string operation, int percentComplete) {
        }
    }
}
