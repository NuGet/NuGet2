using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using NuGet;

namespace NuGetConsole.Host.PowerShell.Implementation {
    internal static class RunspaceExtensions {
        public static Collection<PSObject> Invoke(this Runspace runspace, string command, object[] inputs, bool outputResults) {
            if (string.IsNullOrEmpty(command)) {
                throw new ArgumentNullException("command");
            }

            using (Pipeline pipeline = CreatePipeline(runspace, command, outputResults)) {
                return inputs != null ? pipeline.Invoke(inputs) : pipeline.Invoke();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope",
            Justification="It is disposed in the StateChanged event handler.")]
        public static Pipeline InvokeAsync(
            this Runspace runspace, 
            string command, 
            object[] inputs, 
            bool outputResults, 
            EventHandler<PipelineStateEventArgs> pipelineStateChanged) {
            
            if (string.IsNullOrEmpty(command)) {
                throw new ArgumentNullException("command");
            }

            Pipeline pipeline = CreatePipeline(runspace, command, outputResults);

            pipeline.StateChanged += (sender, e) => {
                pipelineStateChanged.Raise(sender, e);

                // Dispose Pipeline object upon completion
                switch (e.PipelineStateInfo.State) {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        ((Pipeline)sender).Dispose();
                        break;
                }
            };

            if (inputs != null) {
                foreach (var input in inputs) {
                    pipeline.Input.Write(input);
                }
            }

            pipeline.InvokeAsync();
            return pipeline;
        }

        public static string ExtractErrorFromErrorRecord(this Runspace runspace, ErrorRecord record) {
            Pipeline pipeline = runspace.CreatePipeline("$input", false);
            pipeline.Commands.Add("out-string");

            Collection<PSObject> result;
            using (PSDataCollection<object> inputCollection = new PSDataCollection<object>()) {
                inputCollection.Add(record);
                inputCollection.Complete();
                result = pipeline.Invoke(inputCollection);
            }

            if (result.Count > 0) {
                string str = result[0].BaseObject as string;
                if (!string.IsNullOrEmpty(str)) {
                    // Remove \r\n, which is added by the Out-String cmdlet.
                    return str.Substring(0, str.Length - 2);
                }
            }

            return String.Empty;
        }

        private static Pipeline CreatePipeline(Runspace runspace, string command, bool outputResults) {
            Pipeline pipeline = runspace.CreatePipeline(command, addToHistory: true);
            if (outputResults) {
                pipeline.Commands.Add("out-default");
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }
            return pipeline;
        }

        public static ExecutionPolicy GetEffectiveExecutionPolicy(this Runspace runspace) {
            return GetExecutionPolicy(runspace, "Get-ExecutionPolicy");
        }

        public static ExecutionPolicy GetExecutionPolicy(this Runspace runspace, ExecutionPolicyScope scope) {
            return GetExecutionPolicy(runspace, "Get-ExecutionPolicy -Scope " + scope);
        }

        private static ExecutionPolicy GetExecutionPolicy(this Runspace runspace, string command) {
            Collection<PSObject> results = runspace.Invoke(command, null, false);
            if (results.Count > 0) {
                return (ExecutionPolicy)results[0].BaseObject;
            }
            else {
                return ExecutionPolicy.Undefined;
            }
        }

        public static void SetExecutionPolicy(this Runspace runspace, ExecutionPolicy policy, ExecutionPolicyScope scope) {
            string command = string.Format(CultureInfo.InvariantCulture, "Set-ExecutionPolicy {0} -Scope {1} -Force", policy.ToString(), scope.ToString());
            runspace.Invoke(command, null, false);
        }

        public static void ImportModule(this Runspace runspace, string modulePath) {
            runspace.Invoke("Import-Module '" + modulePath + "'", null, false);
        }

        public static void ExecuteScript(this Runspace runspace, string installPath, string scriptPath, IPackage package) {
            string fullPath = Path.Combine(installPath, scriptPath);
            if (File.Exists(fullPath)) {
                string folderPath = Path.GetDirectoryName(fullPath);

                runspace.Invoke(
                   "$__pc_args=@(); $input|%{$__pc_args+=$_}; & '" + fullPath + "' $__pc_args[0] $__pc_args[1] $__pc_args[2]; Remove-Variable __pc_args -Scope 0",
                   new object[] { installPath, folderPath, package },
                   outputResults: true);
            }
        }

        public static void ChangePSDirectory(this Runspace runspace, string directory) {
            if (!String.IsNullOrWhiteSpace(directory)) {
                runspace.Invoke("Set-Location " + directory, null, false);
            }
        }
    }
}