using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace NuGetConsole.Host.PowerShell {
    public interface IPowerShellHost {
        bool IsAsync { get; }
        Collection<PSObject> Invoke(string command, object[] inputs, bool outputResults);
        bool InvokeAsync(string command, object[] inputs, bool outputResults, EventHandler<PipelineStateEventArgs> pipelineStateChanged);
    }
}
