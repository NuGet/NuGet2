using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using EnvDTE;
using EnvDTE80;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using Microsoft.PowerShell;

namespace NuGetConsole.Host.PowerShell.Implementation {

    internal abstract class PowerShellHost : IPowerShellHost, IPathExpansion, IDisposable, IHost {
        public IConsole Console { get; private set; }

        private string _name;
        private object _privateData;
        private Runspace _myRunSpace;
        private MyHost _myHost;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISolutionManager _solutionManager;

        protected PowerShellHost(IConsole console, string name, bool isAsync, object privateData) {
            UtilityMethods.ThrowIfArgumentNull(console);

            this.Console = console;
            this.IsAsync = isAsync;

            // TODO: Take these as ctor arguments
            _packageSourceProvider = ServiceLocator.GetInstance<IPackageSourceProvider>();
            _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();

            _name = name;
            _privateData = privateData;
            IsCommandEnabled = true;
        }

        public bool IsCommandEnabled {
            get;
            private set;
        }

        public void Initialize() {
            // we setup the runspace here, rather than loading it on-demand. This is so that we can
            // load user profile scripts before the command prompt shows up. It also helps with 
            // tab expansion right from the beginning
            SetupRunspace();
            bool successful = LoadStartupScripts();
            if (successful) {
                DisplayDisclaimerText();
                LoadProfilesIntoRunspace(_myRunSpace);
            }
            else {
                IsCommandEnabled = false;
                DisplayGroupPolicyError();
            }
        }

        private void DisplayDisclaimerText() {
            _myHost.UI.WriteLine(VsResources.Console_DisclaimerText);
            _myHost.UI.WriteLine();
        }

        private void DisplayGroupPolicyError() {
            _myHost.UI.WriteErrorLine(VsResources.Console_GroupPolicyError);
        }

        private bool LoadStartupScripts() {
            ExecutionPolicy policy = this.GetEffectiveExecutionPolicy();
            if (policy != ExecutionPolicy.Unrestricted &&
                policy != ExecutionPolicy.RemoteSigned &&
                policy != ExecutionPolicy.Bypass) {

                ExecutionPolicy machinePolicy = this.GetExecutionPolicy(ExecutionPolicyScope.MachinePolicy);
                if (machinePolicy != ExecutionPolicy.Undefined) {
                    return false;
                }

                ExecutionPolicy userPolicy = this.GetExecutionPolicy(ExecutionPolicyScope.UserPolicy);
                if (userPolicy != ExecutionPolicy.Undefined) {
                    return false;
                }

                this.SetExecutionPolicy(ExecutionPolicy.RemoteSigned, ExecutionPolicyScope.Process);
            }

            string extensionLocation = Path.GetDirectoryName(GetType().Assembly.Location);
            string profilePath = Path.Combine(extensionLocation, @"Scripts\Profile.ps1");
            string npackPath = Path.Combine(extensionLocation, @"Scripts\NuGet.psm1");
            string vsPath = Path.Combine(extensionLocation, @"NuGet.VisualStudio.dll");

            this.ImportModule(profilePath);
            this.ImportModule(npackPath);
            this.ImportModule(vsPath);

            return true;
        }

        private void SetupRunspace() {
            if (_myRunSpace != null) {
                return;
            }

            DTE dte = ServiceLocator.GetInstance<DTE>();
            IVsPackageManagerFactory packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();

            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Variables.Add(
                new SessionStateVariableEntry("DTE", (DTE2)dte, "Visual Studio DTE automation object",
                    ScopedItemOptions.AllScope | ScopedItemOptions.Constant));


            initialSessionState.Variables.Add(
                new SessionStateVariableEntry("packageManagerFactory", packageManagerFactory, "Package Manager Factory",
                    ScopedItemOptions.AllScope | ScopedItemOptions.Constant));

            // For debugging, uncomment these lines below. Loading the scripts through InitialSessionState
            // will reveal syntax error information if there is any.
            //
            //string extensionLocation = Path.GetDirectoryName(GetType().Assembly.Location);
            //string profilePath = Path.Combine(extensionLocation, @"Scripts\Profile.ps1");
            //string npackPath = Path.Combine(extensionLocation, @"Scripts\NuGet.psm1");
            //string vsPath = Path.Combine(extensionLocation, @"NuGet.VisualStudio.dll");
            //initialSessionState.ImportPSModule(new string[] { profilePath, npackPath, vsPath });

            _myHost = new MyHost(this, _name, _privateData);
            _myRunSpace = RunspaceFactory.CreateRunspace(_myHost, initialSessionState);

            // if is sync, set UseCurrentThread for Invoke
            if (!IsAsync) {
                _myRunSpace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            }

            _myRunSpace.Open();

            //
            // Set this runspace as DefaultRunspace so I can script DTE events.
            //
            // WARNING: MSDN says this is unsafe. The runspace must not be shared across
            // threads. I need this to be able to use ScriptBlock for DTE events. The
            // ScriptBlock event handlers execute on DefaultRunspace.
            //
            Runspace.DefaultRunspace = _myRunSpace;
        }

        private void LoadProfilesIntoRunspace(Runspace runspace) {
            using (var powerShell = System.Management.Automation.PowerShell.Create()) {
                powerShell.Runspace = runspace;

                PSCommand[] profileCommands = HostUtilities.GetProfileCommands("NuGet");
                foreach (PSCommand command in profileCommands) {
                    try {
                        powerShell.Commands = command;
                        powerShell.AddCommand("out-default");
                        powerShell.Invoke();
                    }
                    catch (RuntimeException ex) {
                        _myHost.UI.WriteLine(
                            "An exception occured while loading one of the profile files. " +
                            "This may happen if the profile calls scripts that require a different environment than the Package Manager Console.");

                        _myHost.UI.WriteErrorLine(ex.Message);
                    }
                }
            }
        }

        ComplexCommand _complexCommand;
        ComplexCommand ComplexCommand {
            get {
                if (_complexCommand == null) {
                    _complexCommand = new ComplexCommand((allLines, lastLine) => {
                        Collection<PSParseError> errors;
                        PSParser.Tokenize(allLines, out errors);

                        // If there is a parse error token whose END is past input END, consider
                        // it a multi-line command.
                        if (errors.Count > 0) {
                            if (errors.Any(e => (e.Token.Start + e.Token.Length) >= allLines.Length)) {
                                return false;
                            }
                        }

                        return true;
                    });
                }
                return _complexCommand;
            }
        }

        public string Prompt {
            get {
                return ComplexCommand.IsComplete ? "PM>" : ">> ";
            }
        }

        public bool Execute(string command) {
            string fullCommand;
            if (ComplexCommand.AddLine(command, out fullCommand) && !string.IsNullOrEmpty(fullCommand)) {
                return ExecuteHost(fullCommand, command);
            }
            else {
                // Add this one piece into history. ExecuteHost adds the last piece.
                this.AddHistory(command, DateTime.Now);
            }

            return false; // constructing multi-line command
        }

        public void Abort() {
            ComplexCommand.Clear();
        }

        protected abstract bool ExecuteHost(string fullCommand, string command);

        protected void ReportError(ErrorRecord record) {
            Pipeline pipeline = CreatePipeline("$input", false);
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
                    _myHost.UI.WriteErrorLine(str.Substring(0, str.Length - 2));
                }

            }
        }

        public string Setting {
            get {
                var activePackageSource = _packageSourceProvider.ActivePackageSource;
                return activePackageSource == null ? null : activePackageSource.Name;
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    throw new ArgumentNullException("value");
                }

                _packageSourceProvider.ActivePackageSource =
                    _packageSourceProvider.GetPackageSources().FirstOrDefault(
                        ps => ps.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            }
        }

        public string[] GetAvailableSettings() {
            return _packageSourceProvider.GetPackageSources().Select(ps => ps.Name).ToArray();
        }

        public string DefaultProject {
            get {
                Debug.Assert(_solutionManager != null);
                return _solutionManager.DefaultProjectName;
            }
            set {
                Debug.Assert(_solutionManager != null);
                _solutionManager.DefaultProjectName = value;
            }
        }

        public string[] GetAvailableProjects() {
            Debug.Assert(_solutionManager != null);

            return (from p in _solutionManager.GetProjects()
                    select p.Name).ToArray();
        }

        #region IPowerShellHost
        public bool IsAsync { get; private set; }

        Pipeline CreatePipeline(string command, bool outputResults) {
            SetupRunspace();
            Pipeline pipeline = _myRunSpace.CreatePipeline();
            pipeline.Commands.AddScript(command);

            if (outputResults) {
                pipeline.Commands.Add("out-default");
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }
            return pipeline;
        }

        public Collection<PSObject> Invoke(string command, object input = null, bool outputResults = true) {
            if (string.IsNullOrEmpty(command)) {
                return null;
            }

            using (Pipeline pipeline = CreatePipeline(command, outputResults)) {
                return input != null ?
                    pipeline.Invoke(new object[] { input }) : pipeline.Invoke();
            }
        }

        public bool InvokeAsync(string command, bool outputResults, EventHandler<PipelineStateEventArgs> pipelineStateChanged) {
            if (string.IsNullOrEmpty(command)) {
                return false;
            }

            Pipeline pipeline = CreatePipeline(command, outputResults);

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

            pipeline.InvokeAsync();
            return true;
        }
        #endregion

        #region ITabExpansion
        public string[] GetExpansions(string line, string lastWord) {
            var query = from s in Invoke(
                            "$__pc_args=@(); $input|%{$__pc_args+=$_}; TabExpansion $__pc_args[0] $__pc_args[1]; Remove-Variable __pc_args -Scope 0",
                            new string[] { line, lastWord },
                            outputResults: false)
                        where s != null
                        select s.ToString();
            return query.ToArray();
        }
        #endregion

        #region IPathExpansion
        public SimpleExpansion GetPathExpansions(string line) {
            PSObject expansion = Invoke(
                "$input|%{$__pc_args=$_}; _TabExpansionPath $__pc_args; Remove-Variable __pc_args -Scope 0",
                line,
                outputResults: false).FirstOrDefault();
            if (expansion != null) {
                int replaceStart = (int)expansion.Properties["ReplaceStart"].Value;
                IList<string> paths = ((IEnumerable<object>)expansion.Properties["Paths"].Value).Select(o => o.ToString()).ToList();
                return new SimpleExpansion(replaceStart, line.Length - replaceStart, paths);
            }

            return null;
        }
        #endregion

        #region IDisposable
        public void Dispose() {
            if (_myRunSpace != null) {
                _myRunSpace.Dispose();
            }
        }
        #endregion
    }

    class SyncPowerShellHost : PowerShellHost {
        public SyncPowerShellHost(IConsole console, string name, object privateData)
            : base(console, name, false, privateData) {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override bool ExecuteHost(string fullCommand, string command) {
            DateTime startExecutionTime = DateTime.Now;

            try {
                Invoke(fullCommand);
            }
            catch (RuntimeException e) {
                ReportError(e.ErrorRecord);
            }
            catch (Exception x) {
                // If an exception pops up, my console becomes unusable. Eat it.
                Debug.Print(x.ToString());
            }

            this.AddHistory(command, startExecutionTime);
            return true;
        }
    }

    class AsyncPowerShellHost : PowerShellHost, IAsyncHost {
        public event EventHandler ExecuteEnd;

        public AsyncPowerShellHost(IConsole console, string name, object privateData)
            : base(console, name, true, privateData) {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override bool ExecuteHost(string fullCommand, string command) {
            DateTime startExecutionTime = DateTime.Now;

            try {
                return InvokeAsync(fullCommand, true, (sender, e) => {
                    switch (e.PipelineStateInfo.State) {
                        case PipelineState.Completed:
                        case PipelineState.Failed:
                        case PipelineState.Stopped:
                            this.AddHistory(command, startExecutionTime);
                            ExecuteEnd.Raise(this, EventArgs.Empty);
                            break;
                    }
                });
            }
            catch (RuntimeException e) {
                ReportError(e.ErrorRecord);
            }
            catch (Exception x) {
                // If an exception pops up, console becomes unusable. Eat it.
                Debug.Print(x.ToString());
            }

            return false; // Error occured, command not executing
        }
    }
}
