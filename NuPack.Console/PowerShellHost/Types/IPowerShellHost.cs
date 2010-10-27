using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace NuGetConsole.Host.PowerShell {
    public interface IPowerShellHost {
        void Initialize();
        bool IsAsync { get; }
        Collection<PSObject> Invoke(string command, object input, bool outputResults);
        bool InvokeAsync(string command, bool outputResults, EventHandler<PipelineStateEventArgs> pipelineStateChanged);
    }
}
