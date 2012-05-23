using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation.Runspaces;
using NuGetConsole.Host.PowerShell.Implementation;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Threading;
using Microsoft.PowerShell;
using System.Globalization;
using System.IO;
using NuGet.VisualStudio;
using NuGet;

namespace NuGetConsole.Host.PowerShell
{
    /// <summary>
    /// Wraps a runspace and protects the invoke method from being called on multiple threads through blocking.
    /// </summary>
    /// <remarks>
    /// Calls to Invoke on this object will block if the runspace is already busy. Calls to InvokeAsync will also block until
    /// the runspace is free. However, it will not block while the pipeline is actually running.
    /// </remarks>
    internal class RunspaceDispatcher : IDisposable
    {
        private Runspace _runspace;
        private object _dispatcherLock = new object();

        public RunspaceAvailability RunspaceAvailability { get { return _runspace.RunspaceAvailability; } }
        
        public RunspaceDispatcher(Runspace runspace)
        {
            _runspace = runspace;
        }

        public void MakeDefault()
        {
            if (Runspace.DefaultRunspace == null)
            {
                lock (_dispatcherLock)
                {
                    if (Runspace.DefaultRunspace == null)
                    {
                        // Set this runspace as DefaultRunspace so I can script DTE events.
                        //
                        // WARNING: MSDN says this is unsafe. The runspace must not be shared across
                        // threads. I need this to be able to use ScriptBlock for DTE events. The
                        // ScriptBlock event handlers execute on DefaultRunspace.

                        Runspace.DefaultRunspace = _runspace;
                    }
                }
            }
        }

        public Collection<PSObject> Invoke(string command, object[] inputs, bool outputResults)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            using (Pipeline pipeline = CreatePipeline(command, outputResults))
            {
                return InvokeCore(pipeline, inputs);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "It is disposed in the StateChanged event handler.")]
        public Pipeline InvokeAsync(
            string command,
            object[] inputs,
            bool outputResults,
            EventHandler<PipelineStateEventArgs> pipelineStateChanged)
        {

            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            Pipeline pipeline = CreatePipeline(command, outputResults);

            pipeline.StateChanged += (sender, e) =>
            {
                pipelineStateChanged.Raise(sender, e);

                // Dispose Pipeline object upon completion
                switch (e.PipelineStateInfo.State)
                {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        ((Pipeline)sender).Dispose();
                        break;
                }
            };

            InvokeCoreAsync(pipeline, inputs);
            return pipeline;
        }

        public string ExtractErrorFromErrorRecord(ErrorRecord record)
        {
            Pipeline pipeline = _runspace.CreatePipeline("$input", false);
            pipeline.Commands.Add("out-string");

            Collection<PSObject> result;
            using (PSDataCollection<object> inputCollection = new PSDataCollection<object>())
            {
                inputCollection.Add(record);
                inputCollection.Complete();
                result = InvokeCore(pipeline, inputCollection);
            }

            if (result.Count > 0)
            {
                string str = result[0].BaseObject as string;
                if (!string.IsNullOrEmpty(str))
                {
                    // Remove \r\n, which is added by the Out-String cmdlet.
                    return str.Substring(0, str.Length - 2);
                }
            }

            return String.Empty;
        }

        private Pipeline CreatePipeline(string command, bool outputResults)
        {
            Pipeline pipeline = _runspace.CreatePipeline(command, addToHistory: true);
            if (outputResults)
            {
                pipeline.Commands.Add("out-default");
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }
            return pipeline;
        }

        public ExecutionPolicy GetEffectiveExecutionPolicy()
        {
            return GetExecutionPolicy("Get-ExecutionPolicy");
        }

        public ExecutionPolicy GetExecutionPolicy(ExecutionPolicyScope scope)
        {
            return GetExecutionPolicy("Get-ExecutionPolicy -Scope " + scope);
        }

        private ExecutionPolicy GetExecutionPolicy(string command)
        {
            Collection<PSObject> results = _runspace.Invoke(command, null, false);
            if (results.Count > 0)
            {
                return (ExecutionPolicy)results[0].BaseObject;
            }
            else
            {
                return ExecutionPolicy.Undefined;
            }
        }

        public void SetExecutionPolicy(ExecutionPolicy policy, ExecutionPolicyScope scope)
        {
            string command = string.Format(CultureInfo.InvariantCulture, "Set-ExecutionPolicy {0} -Scope {1} -Force", policy.ToString(), scope.ToString());
            
            Invoke(command, null, false);
        }

        public void ExecuteScript(string installPath, string scriptPath, IPackage package)
        {
            string fullPath = Path.Combine(installPath, scriptPath);
            if (File.Exists(fullPath))
            {
                string folderPath = Path.GetDirectoryName(fullPath);

                Invoke(
                   "$__pc_args=@(); $input|%{$__pc_args+=$_}; & " + PathHelper.EscapePSPath(fullPath) + " $__pc_args[0] $__pc_args[1] $__pc_args[2]; Remove-Variable __pc_args -Scope 0",
                   new object[] { installPath, folderPath, package },
                   outputResults: true);
            }
        }

        public void ImportModule(string modulePath)
        {
            Invoke("Import-Module " + PathHelper.EscapePSPath(modulePath), null, false);
        }

        public void ChangePSDirectory(string directory)
        {
            if (!String.IsNullOrWhiteSpace(directory))
            {
                Invoke("Set-Location " + PathHelper.EscapePSPath(directory), null, false);
            }
        }

        public void Dispose()
        {
            _runspace.Dispose();
        }

        // Dispatcher synchronization methods
        private Collection<PSObject> InvokeCore(Pipeline pipeline, IEnumerable<object> inputs)
        {
            lock (_dispatcherLock)
            {
                return inputs == null ? pipeline.Invoke() : pipeline.Invoke(inputs);
            }
        }

        private void InvokeCoreAsync(Pipeline pipeline, IEnumerable<object> inputs)
        {
            pipeline.StateChanged += (sender, e) =>
            {
                switch (e.PipelineStateInfo.State)
                {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        // Release the dispatcher lock
                        Monitor.Exit(_dispatcherLock);
                        Monitor.Pulse(_dispatcherLock);
                        break;
                }
            };

            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    pipeline.Input.Write(input);
                }
            }

            // Take the dispatcher lock and invoke the pipeline
            // REVIEW: This could probably be done in a Task so that we can return to the caller before even taking the dispatcher lock
            Monitor.Enter(_dispatcherLock);
            pipeline.InvokeAsync();
        }
    }
}
