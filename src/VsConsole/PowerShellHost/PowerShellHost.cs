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
using Microsoft.PowerShell;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGet;

namespace NuGetConsole.Host.PowerShell.Implementation {

    internal abstract class PowerShellHost : IPowerShellHost, IPathExpansion, IDisposable, IHost {
        public IConsole Console { get; private set; }

        private string _name;
        private object _privateData;
        private Runspace _myRunSpace;
        private MyHost _myHost;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly ISolutionManager _solutionManager;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private Pipeline _executingPipeline;

        protected PowerShellHost(IConsole console, string name, bool isAsync, object privateData) {
            UtilityMethods.ThrowIfArgumentNull(console);

            this.Console = console;
            this.IsAsync = isAsync;

            // TODO: Take these as ctor arguments
            _packageSourceProvider = ServiceLocator.GetInstance<IPackageSourceProvider>();
            _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
            _packageManagerFactory = ServiceLocator.GetInstance<IVsPackageManagerFactory>();

            _name = name;
            _privateData = privateData;
            IsCommandEnabled = true;
        }

        public bool IsCommandEnabled {
            get;
            private set;
        }

        /// <summary>
        /// Doing all necessary initialization works before the console accepts user inputs
        /// </summary>
        public void Initialize() {
            // we setup the runspace here, rather than loading it on-demand. This is so that we can
            // load user profile scripts before the command prompt shows up. It also helps with 
            // tab expansion right from the beginning
            SetupRunspace();
            bool successful = LoadStartupScripts();
            if (successful) {
                if (Console.ShowDisclaimerHeader) {
                    DisplayDisclaimerAndHelpText();
                }

                LoadProfilesIntoRunspace(_myRunSpace);

                ExecuteInitScripts();
                _solutionManager.SolutionOpened += (o, e) => ExecuteInitScripts();
            }
            else {
                IsCommandEnabled = false;
                DisplayGroupPolicyError();
            }
        }

        private void ExecuteInitScripts() {
            if (!String.IsNullOrEmpty(_solutionManager.SolutionDirectory)) {
                var packageManager = (VsPackageManager)_packageManagerFactory.CreatePackageManager();
                var localRepository = packageManager.LocalRepository;

                // invoke init.ps1 files in the order of package dependency.
                // if A -> B, we invoke B's init.ps1 before A's.

                var sorter = new PackageSorter();
                var sortedPackages = sorter.GetPackagesByDependencyOrder(localRepository);

                foreach (var package in sortedPackages) {
                    string installPath = packageManager.PathResolver.GetInstallPath(package);

                    this.AddPathToEnvironment(Path.Combine(installPath, "tools"));
                    this.ExecuteScript(installPath, "tools\\init.ps1", package);
                }
            }
        }

        private void DisplayDisclaimerAndHelpText() {
            _myHost.UI.WriteLine(VsResources.Console_DisclaimerText);
            _myHost.UI.WriteLine();
            _myHost.UI.WriteLine(VsResources.Console_HelpText);
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
            string nugetPath = Path.Combine(extensionLocation, @"NuGet.psd1");
            this.ImportModule(nugetPath);

            return true;
        }

        private void SetupRunspace() {
            if (_myRunSpace != null) {
                return;
            }

            DTE dte = ServiceLocator.GetInstance<DTE>();

            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Variables.Add(
                new SessionStateVariableEntry("DTE", (DTE2)dte, "Visual Studio DTE automation object",
                    ScopedItemOptions.AllScope | ScopedItemOptions.Constant));

            // The constructor expects Tuple<string, object>. We can’t let Tuple.Create() infer the type here because it will 
            // infer it as Tuple<string, PackageManagerFactory>, which can’t be converted to Tuple<string, object>.
            // Thus we have to provide explicity generic type arguments for Tuple.Create().
            _myHost = new MyHost(this, _name, _privateData, Tuple.Create<string, object>("packageManagerFactory", _packageManagerFactory));
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
            if (_executingPipeline != null) {
                _executingPipeline.StopAsync();
            }
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

        protected void ReportError(Exception exception) {
            _myHost.UI.WriteErrorLine((exception.InnerException ?? exception).Message);
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
                _executingPipeline = pipeline;
                try {
                    return input != null ? pipeline.Invoke(new object[] { input }) : pipeline.Invoke();
                }
                finally {
                    _executingPipeline = null;
                }
            }
        }

        public bool InvokeAsync(string command, bool outputResults, EventHandler<PipelineStateEventArgs> pipelineStateChanged) {
            if (string.IsNullOrEmpty(command)) {
                return false;
            }

            Debug.Assert(_executingPipeline == null);

            Pipeline pipeline = CreatePipeline(command, outputResults);

            pipeline.StateChanged += (sender, e) => {
                pipelineStateChanged.Raise(sender, e);

                // Dispose Pipeline object upon completion
                switch (e.PipelineStateInfo.State) {
                    case PipelineState.Completed:
                    case PipelineState.Failed:
                    case PipelineState.Stopped:
                        _executingPipeline = null;
                        ((Pipeline)sender).Dispose();
                        break;
                }
            };

            _executingPipeline = pipeline;

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
                        select (s == null ? null : s.ToString());
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
                            if (e.PipelineStateInfo.Reason != null) {
                                ReportError(e.PipelineStateInfo.Reason);
                            }

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