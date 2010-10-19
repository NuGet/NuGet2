namespace NuPack {
    public class NullLogger : ILogger {
        public static readonly ILogger Instance = new NullLogger();

        public void Log(MessageLevel level, string message, params object[] args) {
        }
    }
}
