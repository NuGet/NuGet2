using System;

namespace NuPack.VisualStudio.Cmdlets {
    internal class LoggerDisposer : IDisposable {
        private dynamic _loggerHost;

        public LoggerDisposer(dynamic loggerHost, ILogger logger) {
            _loggerHost = loggerHost;
            loggerHost.Logger = logger;
        }

        public void Dispose() {
            _loggerHost.Logger = null;
        }
    }
}