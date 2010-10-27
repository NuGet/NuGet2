using Microsoft.Win32;

namespace NuGetConsole.Host.PowerShellProvider {

    internal static class RegistryHelper {

        /// <summary>
        /// Detects if PowerShell 2.0 runtime is installed.
        /// </summary>
        /// <remarks>
        /// Detection logic is obtained from here: 
        /// http://blogs.msdn.com/b/powershell/archive/2009/06/25/detection-logic-poweshell-installation.aspx
        /// </remarks>
        public static bool CheckIfPowerShell2Installed() {

            string keyPath = @"SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine";

            RegistryKey currentKey = Registry.LocalMachine;

            foreach (string subKeyName in keyPath.Split('\\')) {
                currentKey = currentKey.OpenSubKey(subKeyName);
                if (currentKey == null) {
                    return false;
                }
            }

            string keyValue = (string)currentKey.GetValue("PowerShellVersion");

            // TODO: Do a better check to be resilient against future version of PS
            return (keyValue == "2.0");
        }
    }
}
