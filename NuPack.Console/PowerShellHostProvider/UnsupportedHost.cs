using System;

namespace NuGetConsole.Host.PowerShellProvider {

    /// <summary>
    /// This host is used when PowerShell 2.0 runtime is not installed in the system. It's basically a no-op host.
    /// </summary>
    internal class UnsupportedHost : IHost {

        public UnsupportedHost(IConsole console) {
            // display the error message at the beginning
            console.Write(Resources.Host_PSNotInstalled, System.Windows.Media.Colors.Red, null);
        }

        public bool IsCommandEnabled {
            get {
                return false;
            }
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

        public string Setting {
            get {
                return String.Empty;
            }
            set {
            }
        }

        public string[] GetAvailableSettings() {
            return new string[0];
        }

        public string DefaultProject {
            get {
                return String.Empty;
            }
            set {
            }
        }

        public string[] GetAvailableProjects() {
            return new string[0];
        }
    }
}
