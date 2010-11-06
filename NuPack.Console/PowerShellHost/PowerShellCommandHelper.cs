using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;

namespace NuGetConsole.Host.PowerShell {
    internal class PowerShellCommandHelper {

        private IPowerShellHost _host;

        public PowerShellCommandHelper(IPowerShellHost host) {
            Debug.Assert(host != null);
            _host = host;
        }

        public ExecutionPolicy GetEffectiveExecutionPolicy() {
            return GetExecutionPolicy("Get-ExecutionPolicy");
        }

        public ExecutionPolicy GetExecutionPolicy(ExecutionPolicyScope scope) {
            return GetExecutionPolicy("Get-ExecutionPolicy -Scope " + scope);
        }

        private ExecutionPolicy GetExecutionPolicy(string command) {
            Collection<PSObject> results = _host.Invoke(command, null, false);
            if (results.Count > 0) {
                return (ExecutionPolicy)results[0].BaseObject;
            }
            else {
                return ExecutionPolicy.Undefined;
            }
        }

        public void SetExecutionPolicy(ExecutionPolicy policy, ExecutionPolicyScope scope) {
            string command = string.Format(CultureInfo.InvariantCulture, "Set-ExecutionPolicy {0} -Scope {1} -Force", policy.ToString(), scope.ToString());
            _host.Invoke(command, null, false);
        }

        public void ImportModule(string modulePath) {
            _host.Invoke("Import-Module '" + modulePath + "'", null, false);
        }

        public void AddHistory(string command, DateTime startExecutionTime) {
            // PowerShell.exe doesn't add empty commands into execution history. Do the same.
            if (!string.IsNullOrEmpty(command) && !string.IsNullOrEmpty(command.Trim())) {
                DateTime endExecutionTime = DateTime.Now;
                PSObject historyInfo = new PSObject();
                historyInfo.Properties.Add(new PSNoteProperty("CommandLine", command), true);
                historyInfo.Properties.Add(new PSNoteProperty("ExecutionStatus", PipelineState.Completed), true);
                historyInfo.Properties.Add(new PSNoteProperty("StartExecutionTime", startExecutionTime), true);
                historyInfo.Properties.Add(new PSNoteProperty("EndExecutionTime", endExecutionTime), true);
                _host.Invoke("$input | Add-History", historyInfo, outputResults: false);
            }
        }
    }
}