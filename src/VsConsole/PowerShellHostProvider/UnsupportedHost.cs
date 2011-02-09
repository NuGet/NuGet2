using System;

namespace NuGetConsole.Host.PowerShellProvider {

    /// <summary>
    /// This host is used when PowerShell 2.0 runtime is not installed in the system. It's basically a no-op host.
    /// </summary>
    internal class UnsupportedHost : IHost {

        private IConsole _console;

        public UnsupportedHost(IConsole console) {
            _console = console;
        }

        public bool IsCommandEnabled {
            get {
                return false;
            }
        }

        public void Initialize() {
            // display the error message at the beginning
            _console.Write(Resources.Host_PSNotInstalled, System.Windows.Media.Colors.Red, null);
        }

        public string Prompt {
            get {
                return String.Empty;
            }
        }

        public bool Execute(string command) {
            return false;
        }

        public void Abort() {
        }

        public IHostSettings Settings {
            get {
                return null;
            }
        }
    }
}
