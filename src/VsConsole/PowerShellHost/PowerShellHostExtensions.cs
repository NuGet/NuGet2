using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using NuGet;

namespace NuGetConsole.Host.PowerShell.Implementation {

    internal static class PowerShellHostExtensions {

        public static ExecutionPolicy GetEffectiveExecutionPolicy(this PowerShellHost host) {
            return GetExecutionPolicy(host, "Get-ExecutionPolicy");
        }

        public static ExecutionPolicy GetExecutionPolicy(this PowerShellHost host, ExecutionPolicyScope scope) {
            return GetExecutionPolicy(host, "Get-ExecutionPolicy -Scope " + scope);
        }

        private static ExecutionPolicy GetExecutionPolicy(PowerShellHost host, string command) {
            Collection<PSObject> results = host.Invoke(command, null, false);
            if (results.Count > 0) {
                return (ExecutionPolicy)results[0].BaseObject;
            }
            else {
                return ExecutionPolicy.Undefined;
            }
        }

        public static void SetExecutionPolicy(this PowerShellHost host, ExecutionPolicy policy, ExecutionPolicyScope scope) {
            string command = string.Format(CultureInfo.InvariantCulture, "Set-ExecutionPolicy {0} -Scope {1} -Force", policy.ToString(), scope.ToString());
            host.Invoke(command, null, false);
        }

        public static void ImportModule(this PowerShellHost host, string modulePath) {
            host.Invoke("Import-Module '" + modulePath + "'", null, false);
        }

        public static void AddHistory(this PowerShellHost host, string command, DateTime startExecutionTime) {
            // PowerShell.exe doesn't add empty commands into execution history. Do the same.
            if (!String.IsNullOrWhiteSpace(command)) {
                DateTime endExecutionTime = DateTime.Now;
                PSObject historyInfo = new PSObject();
                historyInfo.Properties.Add(new PSNoteProperty("CommandLine", command), true);
                historyInfo.Properties.Add(new PSNoteProperty("ExecutionStatus", PipelineState.Completed), true);
                historyInfo.Properties.Add(new PSNoteProperty("StartExecutionTime", startExecutionTime), true);
                historyInfo.Properties.Add(new PSNoteProperty("EndExecutionTime", endExecutionTime), true);
                host.Invoke("$input | Add-History", historyInfo, outputResults: false);
            }
        }

        public static void ExecuteScript(this PowerShellHost host, string installPath, string scriptPath, IPackage package) {
            string fullPath = Path.Combine(installPath, scriptPath);
            if (File.Exists(fullPath)) {
                string folderPath = Path.GetDirectoryName(fullPath);

                host.Invoke(
                   "$__pc_args=@(); $input|%{$__pc_args+=$_}; & '" + fullPath + "' $__pc_args[0] $__pc_args[1] $__pc_args[2]; Remove-Variable __pc_args -Scope 0",
                   new object[] { installPath, folderPath, package},
                   outputResults: true);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Usage", 
            "CA1801:ReviewUnusedParameters", 
            MessageId = "host",
            Justification = "We want to make this as an extension method.")]
        public static void AddPathToEnvironment(this PowerShellHost host, string path) {
            if (Directory.Exists(path)) {
                string environmentPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Process);
                environmentPath = environmentPath + ";" + path;
                Environment.SetEnvironmentVariable("path", environmentPath, EnvironmentVariableTarget.Process);
            }
        }
    }
}