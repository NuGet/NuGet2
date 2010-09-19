namespace NuPack {
    public class NullLogger : ILogger {
        internal static readonly ILogger Instance = new NullLogger();

        public void Log(MessageLevel level, string message, params object[] args) {
        }
    }
}
