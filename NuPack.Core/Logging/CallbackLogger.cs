namespace NuPack {
    using System;
    using System.Diagnostics;
    using System.Globalization;

    public sealed class CallbackLogger : ILogger {
        private Action<MessageLevel, string> _log;

        public CallbackLogger(Action<MessageLevel, string> log) {
            if (log == null) {
                throw new ArgumentNullException("log");
            }
            _log = log;
        }

        public void Log(MessageLevel level, string message, params object[] args) {
            Debug.Assert(_log != null);
            _log(level, String.Format(CultureInfo.CurrentCulture, message, args));
        }
    }
}