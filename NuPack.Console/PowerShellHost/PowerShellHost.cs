using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using EnvDTE80;
using NuPack;
using NuPack.VisualStudio;
using NuPack.VisualStudio.Resources;

namespace NuPackConsole.Host.PowerShell.Implementation
{

    abstract class PowerShellHost : IPowerShellHost, IPathExpansion, IDisposable, IHost
    {
        private Pipeline CurrentPipeline { get; set; }
        public IConsole Console { get; private set; }
        
        private string _name;
        private Action<InitialSessionState> _init;
        private object _privateData;
        private Runspace _myRunSpace;
        private MyHost _myHost;
        private SolutionProjectsHelper _solutionHelper;
        private VSPackageSourceProvider _packageSourceProvider;

        protected PowerShellHost(IConsole console, DTE2 dte, string name, bool isAsync, Action<InitialSessionState> init, object privateData)
        {
            UtilityMethods.ThrowIfArgumentNull(console);

            this.Console = console;
            this.IsAsync = isAsync;

            _packageSourceProvider = VSPackageSourceProvider.GetSourceProvider(dte);

            _solutionHelper = new SolutionProjectsHelper(dte, this);
            _name = name;
            _init = init;
            _privateData = privateData;
        }

        public void Initialize()
        {
            // we setup the runspace here, rather than loading it on-demand. This is so that we can
            // load user profile scripts before the command prompt shows up. It also helps with 
            // tab expansion right from the beginning
            SetupRunspace();

            LoadStartupScripts();

            DisplayDisclaimerText();

            LoadProfilesIntoRunspace(_myRunSpace);

            // IMPORTANT: should only register for DTE solution event after we have setup the Runspace. 
            // Otherwise, some weird COM exceptions will occur.
            _solutionHelper.RegisterSolutionEvents();
        }

        private void DisplayDisclaimerText() {
            // use WriteWarningLine() method to display the message in pink
            _myHost.UI.WriteLine(VsResources.DisclaimerText);
            _myHost.UI.WriteLine();
        }

        private void LoadStartupScripts() {
            string extensionLocation = Path.GetDirectoryName(GetType().Assembly.Location);
            string profilePath = Path.Combine(extensionLocation, @"Scripts\Profile.ps1");
            string npackPath = Path.Combine(extensionLocation, @"Scripts\nupack.ps1");

            Invoke("Set-ExecutionPolicy RemoteSigned -Scope Process -Force", null, false);
            Invoke("Import-Module '" + profilePath + "'", null, false);
            Invoke("Import-Module '" + npackPath + "'", null, false);
        }

        private void SetupRunspace()
        {
            if (_myRunSpace != null)
            {
                return;
            }
            
            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
            if (_init != null)
            {
                _init(initialSessionState);
            }

            initialSessionState.Variables.Add(
                new SessionStateVariableEntry(
                    "PackageSourceStore", 
                    _packageSourceProvider, 
                    null, 
                    ScopedItemOptions.ReadOnly));

            initialSessionState.Variables.Add(
                new SessionStateVariableEntry(
                    "NuPackDisclaimerText",
                    VsResources.InstallSuccessDisclaimerText,
                    null,
                    ScopedItemOptions.ReadOnly));

            // For debugging, uncomment these lines below. Loading the scripts through InitialSessionState
            // will reveal syntax error information if there is any.
            //
            //string extensionLocation = Path.GetDirectoryName(GetType().Assembly.Location);
            //string profilePath = Path.Combine(extensionLocation, @"Scripts\Profile.ps1");
            //string npackPath = Path.Combine(extensionLocation, @"Scripts\nupack.ps1");
            //initialSessionState.ImportPSModule(new string[] { profilePath, npackPath });

            _myHost = new MyHost(this, _name, _privateData);
            _myRunSpace = RunspaceFactory.CreateRunspace(_myHost, initialSessionState);

            // if is sync, set UseCurrentThread for Invoke
            if (!IsAsync)
            {
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

        private void LoadProfilesIntoRunspace(Runspace _myRunSpace)
        {
            var powerShell = System.Management.Automation.PowerShell.Create();
            powerShell.Runspace = _myRunSpace;
            
            PSCommand[] profileCommands = HostUtilities.GetProfileCommands("NuPack");
            foreach (PSCommand command in profileCommands)
            {
                try
                {
                    powerShell.Commands = command;
                    powerShell.AddCommand("out-default");
                    powerShell.Invoke();
                }
                catch (RuntimeException ex)
                {
                    _myHost.UI.WriteLine(
                        "An exception occured while loading one of the profile files. " +
                        "This may happen if the profile calls scripts that require a different environment than the Package Manager Console.");

                    _myHost.UI.WriteErrorLine(ex.Message);
                }
            }
        }

        ComplexCommand _complexCommand;
        ComplexCommand ComplexCommand
        {
            get
            {
                if (_complexCommand == null)
                {
                    _complexCommand = new ComplexCommand((allLines, lastLine) =>
                    {
                        Collection<PSParseError> errors;
                        PSParser.Tokenize(allLines, out errors);

                        // If there is a parse error token whose END is past input END, consider
                        // it a multi-line command.
                        if (errors.Count > 0)
                        {
                            if (errors.Any(e => (e.Token.Start + e.Token.Length) >= allLines.Length))
                            {
                                return false;
                            }
                        }

                        return true;
                    });
                }
                return _complexCommand;
            }
        }

        public string Prompt
        {
            get 
			{ 
				return ComplexCommand.IsComplete ? "PM>" : ">> "; 
			}
        }

        public bool Execute(string command)
        {
            string fullCommand;
            if (ComplexCommand.AddLine(command, out fullCommand) && !string.IsNullOrEmpty(fullCommand))
            {
                return ExecuteHost(fullCommand, command);
            }
            else
            {
                // Add this one piece into history. ExecuteHost adds the last piece.
                AddHistory(command, DateTime.Now);
            }

            return false; // constructing multi-line command
        }

        public void Abort()
        {
            ComplexCommand.Clear();
        }

        protected abstract bool ExecuteHost(string fullCommand, string command);

        protected void AddHistory(string command, DateTime startExecutionTime)
        {
            // PowerShell.exe doesn't add empty commands into execution history. Do the same.
            if (!string.IsNullOrEmpty(command) && !string.IsNullOrEmpty(command.Trim()))
            {
                DateTime endExecutionTime = DateTime.Now;
                PSObject historyInfo = new PSObject();
                historyInfo.Properties.Add(new PSNoteProperty("CommandLine", command), true);
                historyInfo.Properties.Add(new PSNoteProperty("ExecutionStatus", PipelineState.Completed), true);
                historyInfo.Properties.Add(new PSNoteProperty("StartExecutionTime", startExecutionTime), true);
                historyInfo.Properties.Add(new PSNoteProperty("EndExecutionTime", endExecutionTime), true);
                Invoke("$input | Add-History", historyInfo, outputResults: false);
            }
        }

        protected void ReportError(ErrorRecord record)
        {
            Pipeline pipeline = CreatePipeline("$input", false);
            pipeline.Commands.Add("out-string");

            Collection<PSObject> result;
            PSDataCollection<object> inputCollection = new PSDataCollection<object>();
            inputCollection.Add(record);
            inputCollection.Complete();
            result = pipeline.Invoke(inputCollection);

            if (result.Count > 0)
            {
                string str = result[0].BaseObject as string;
                if (!string.IsNullOrEmpty(str))
                {
                    // Remove \r\n, which is added by the Out-String cmdlet.
                    _myHost.UI.WriteErrorLine(str.Substring(0, str.Length - 2));
                }

            }
        }

        public string Setting
        {
            get
            {
                var activePackageSource = _packageSourceProvider.ActivePackageSource;
                return activePackageSource == null ? null : activePackageSource.Source;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) {
                    throw new ArgumentNullException();
                }

                _packageSourceProvider.ActivePackageSource =
                    _packageSourceProvider.GetPackageSources().FirstOrDefault(
                        ps => ps.Source.Equals(value, StringComparison.OrdinalIgnoreCase));
            }
        }

        public string[] AvailableSettings 
        {
            get 
            {
                return _packageSourceProvider.GetPackageSources().Select(ps => ps.Source).ToArray();
            }
        }

        public string DefaultProject {
            get {
                return (string)InvokeResult("get-variable DefaultProjectName");
            }
            set {
                Invoke("_SetDefaultProjectInternal '" + (value ?? String.Empty) + "'", null, false);
            }
        }

        public string[] AvailableProjects {
            get {
                return _solutionHelper.GetCurrentProjectNames();
            }
        }

        private object InvokeResult(string command) {
            if (_myHost != null)
            {
                Collection<PSObject> results = Invoke(command, null, false);
                if (results != null && results.Count > 0)
                {
                    return (string)results[0].Properties["Value"].Value;
                }
            }
            
            return null;
        }

        #region IPowerShellHost
        public bool IsAsync { get; private set; }

        Pipeline CreatePipeline(string command, bool outputResults)
        {
            SetupRunspace();
            Pipeline pipeline = _myRunSpace.CreatePipeline();
            pipeline.Commands.AddScript(command);

            if (outputResults)
            {
                pipeline.Commands.Add("out-default");
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }
            return pipeline;
        }

        public Collection<PSObject> Invoke(string command, object input = null, bool outputResults = true)
        {
            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            using (Pipeline pipeline = CreatePipeline(command, outputResults))
            {
                return input != null ?
                    pipeline.Invoke(new object[] { input }) : pipeline.Invoke();
            }
        }

        public bool InvokeAsync(string command, bool outputResults, EventHandler<PipelineStateEventArgs> pipelineStateChanged)
        {
            if (string.IsNullOrEmpty(command))
            {
                return false;
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

            pipeline.InvokeAsync();
            return true;
        }
        #endregion

        #region ITabExpansion
        public string[] GetExpansions(string line, string lastWord)
        {
            var query = from s in Invoke(
                            "$__pc_args=@(); $input|%{$__pc_args+=$_}; TabExpansion $__pc_args[0] $__pc_args[1]; Remove-Variable __pc_args -Scope 0",
                            new string[] { line, lastWord },
                            outputResults: false)
                        select s.ToString();
            return query.ToArray();
        }
        #endregion

        #region IPathExpansion
        public SimpleExpansion GetPathExpansions(string line)
        {
            PSObject expansion = Invoke(
                "$input|%{$__pc_args=$_}; _TabExpansionPath $__pc_args; Remove-Variable __pc_args -Scope 0",
                line,
                outputResults: false).FirstOrDefault();
            if (expansion != null)
            {
                int replaceStart = (int)expansion.Properties["ReplaceStart"].Value;
                string[] paths = ((IEnumerable<object>)expansion.Properties["Paths"].Value).Select(o => o.ToString()).ToArray();
                return new SimpleExpansion(replaceStart, line.Length - replaceStart, paths);
            }

            return null;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_myRunSpace != null)
            {
                _myRunSpace.Dispose();
            }
        }
        #endregion
    }

    class SyncPowerShellHost : PowerShellHost
    {
        public SyncPowerShellHost(IConsole console, DTE2 dte, string name, Action<InitialSessionState> init, object privateData)
            : base(console, dte, name, false, init, privateData)
        {
        }

        protected override bool ExecuteHost(string fullCommand, string command)
        {
            DateTime startExecutionTime = DateTime.Now;

            try
            {
                Invoke(fullCommand);
            }
            catch (RuntimeException e)
            {
                ReportError(e.ErrorRecord);
            }
            catch (Exception x)
            {
                // If an exception pops up, my console becomes unusable. Eat it.
                Debug.Print(x.ToString());
            }
            
            AddHistory(command, startExecutionTime);
            return true;
        }
    }

    class AsyncPowerShellHost : PowerShellHost, IAsyncHost
    {
        public event EventHandler ExecuteEnd;

        public AsyncPowerShellHost(IConsole console, DTE2 dte, string name, Action<InitialSessionState> init, object privateData)
            : base(console, dte, name, true, init, privateData)
        {
        }

        protected override bool ExecuteHost(string fullCommand, string command)
        {
            DateTime startExecutionTime = DateTime.Now;

            try
            {
                return InvokeAsync(fullCommand, true, (sender, e) =>
                {
                    switch (e.PipelineStateInfo.State)
                    {
                        case PipelineState.Completed:
                        case PipelineState.Failed:
                        case PipelineState.Stopped:
                            AddHistory(command, startExecutionTime);
                            ExecuteEnd.Raise(this, EventArgs.Empty);
                            break;
                    }
                });
            }
            catch (RuntimeException e)
            {
                ReportError(e.ErrorRecord);
            }
            catch (Exception x)
            {
                // If an exception pops up, my console becomes unusable. Eat it.
                Debug.Print(x.ToString());
            }

            return false; // Error occured, command not executing
        }
    }
}