extern alias dialog10;

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Options;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGetConsole;
using NuGetConsole.Implementation;

using VS10ManagePackageDialog = dialog10::NuGet.Dialog.PackageManagerWindow;

namespace NuGet.Tools {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", NuGetPackage.ProductVersion, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(PowerConsoleToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}",      // this is the guid of the Output tool window, which is present in both VS and VWD
        Orientation = ToolWindowOrientation.Right)]
    [ProvideOptionPage(typeof(PackageSourceOptionsPage), "Package Manager", "Package Sources", 113, 114, true)]
    [ProvideOptionPage(typeof(GeneralOptionPage), "Package Manager", "General", 113, 115, true)]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [FontAndColorsRegistration(
        "Package Manager Console",
        NuGetConsole.Implementation.GuidList.GuidPackageManagerConsoleFontAndColorCategoryString,
        "{" + GuidList.guidNuGetPkgString + "}")]
    [Guid(GuidList.guidNuGetPkgString)]
    public sealed class NuGetPackage : Microsoft.VisualStudio.Shell.Package {
        // This product version will be updated by the build script to match the daily build version.
        // It is displayed in the Help - About box of Visual Studio
        public const string ProductVersion = "1.2.0.0";
        private static readonly string[] _visualizerSupportedSKUs = new[] { "Premium", "Ultimate" };

        private uint _debuggingContextCookie, _solutionBuildingContextCookie;
        private DTE _dte;
        private IConsoleStatus _consoleStatus;
        private IVsMonitorSelection _vsMonitorSelection;
        private bool? _isVisualizerSupported;

        public NuGetPackage() {
            HttpClient.DefaultCredentialProvider = new VSRequestCredentialProvider();
        }

        private bool IsVisualizerSupported {
            get {
                if (!_isVisualizerSupported == null) {
                    _isVisualizerSupported = _visualizerSupportedSKUs.Contains(_dte.Edition, StringComparer.OrdinalIgnoreCase);
                }
                return _isVisualizerSupported.Value;
            }
        }

        private void ShowToolWindow(object sender, EventArgs e) {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(PowerConsoleToolWindow), 0, true);
            if ((null == window) || (null == window.Frame)) {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// Executes the NuGet Visualizer.
        /// </summary>
        private void ExecuteVisualizer(object sender, EventArgs e) {
            var visualizer = new NuGet.Dialog.Visualizer(
                ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                ServiceLocator.GetInstance<ISolutionManager>());
            string outputFile = visualizer.CreateGraph();
            _dte.ItemOperations.OpenFile(outputFile);
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e) {
            if (_vsMonitorSelection.GetIsSolutionNodeSelected()) {
                ShowManageLibraryPackageDialog(null);
            }
            else {
                Project project = _vsMonitorSelection.GetActiveProject();
                if (project != null && !project.IsUnloaded() && project.IsSupported()) {
                    ShowManageLibraryPackageDialog(project);
                }
                else {
                    // show error message when no supported project is selected.
                    string projectName = project != null ? project.Name : String.Empty;

                    string errorMessage;
                    if (String.IsNullOrEmpty(projectName)) {
                        errorMessage = Resources.NoProjectSelected;
                    }
                    else {
                        errorMessage = String.Format(CultureInfo.CurrentCulture, VsResources.DTE_ProjectUnsupported, projectName);
                    }

                    MessageHelper.ShowWarningMessage(
                        errorMessage,
                        NuGet.Dialog.Resources.Dialog_MessageBoxTitle);
                }
            }
        }

        private void ShowManageLibraryPackageForSolutionDialog(object sender, EventArgs e) {
            ShowManageLibraryPackageDialog(null);
        }

        private static void ShowManageLibraryPackageDialog(Project project) {
            DialogWindow window = new VS10ManagePackageDialog(project);
            try {
                window.ShowModal();
            }
            catch (TargetInvocationException exception) {
                MessageHelper.ShowErrorMessage(exception, NuGet.Dialog.Resources.Dialog_MessageBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args) {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = !IsIDEInDebuggingOrBuildingContext() && (_vsMonitorSelection.GetIsSolutionNodeSelected() || HasActiveLoadedSupportedProject);
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = !_consoleStatus.IsBusy;
        }

        private void BeforeQueryStatusForAddPackageForSolutionDialog(object sender, EventArgs args) {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = !IsIDEInDebuggingOrBuildingContext();
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = !_consoleStatus.IsBusy;
        }

        private void QueryStatusForVisualizer(object sender, EventArgs args) {
            OleMenuCommand command = (OleMenuCommand)sender;
            var solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
            command.Visible = solutionManager.IsSolutionOpen && IsVisualizerSupported;
        }

        private bool IsIDEInDebuggingOrBuildingContext() {
            int pfActive;
            int result = _vsMonitorSelection.IsCmdUIContextActive(_debuggingContextCookie, out pfActive);
            if (result == VSConstants.S_OK && pfActive > 0) {
                return true;
            }

            result = _vsMonitorSelection.IsCmdUIContextActive(_solutionBuildingContextCookie, out pfActive);
            if (result == VSConstants.S_OK && pfActive > 0) {
                return true;
            }

            return false;
        }

        private void ShowPackageSourcesOptionPage(object sender, EventArgs args) {
            ShowOptionPageSafe(typeof(PackageSourceOptionsPage));
        }

        private void ShowGeneralSettingsOptionPage(object sender, EventArgs args) {
            ShowOptionPageSafe(typeof(GeneralOptionPage));
        }

        private void ShowOptionPageSafe(Type optionPageType) {
            try {
                ShowOptionPage(optionPageType);
            }
            catch (Exception exception) {
                MessageHelper.ShowErrorMessage(exception,
                                               NuGet.Dialog.Resources.Dialog_MessageBoxTitle);

                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            base.Initialize();

            // get the UI context cookie for the debugging mode
            _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            // get debugging context cookie
            Guid debuggingContextGuid = VSConstants.UICONTEXT_Debugging;
            _vsMonitorSelection.GetCmdUIContextCookie(ref debuggingContextGuid, out _debuggingContextCookie);

            // get the solution building cookie
            Guid solutionBuildingContextGuid = VSConstants.UICONTEXT_SolutionBuilding;
            _vsMonitorSelection.GetCmdUIContextCookie(ref solutionBuildingContextGuid, out _solutionBuildingContextCookie);

            _dte = ServiceLocator.GetInstance<DTE>();
            _consoleStatus = ServiceLocator.GetInstance<IConsoleStatus>();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();
        }

        private void AddMenuCommandHandlers() {
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs) {
                // menu command for opening Package Manager Console
                CommandID toolwndCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, (int)PkgCmdIDList.cmdidPowerConsole);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);

                // menu command for opening Manage NuGet packages dialog
                CommandID managePackageDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, (int)PkgCmdIDList.cmdidAddPackageDialog);
                OleMenuCommand managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, BeforeQueryStatusForAddPackageDialog, managePackageDialogCommandID);
                mcs.AddCommand(managePackageDialogCommand);

                // menu command for opening "Manage NuGet packages for solution" dialog
                CommandID managePackageForSolutionDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, (int)PkgCmdIDList.cmdidAddPackageDialogForSolution);
                OleMenuCommand managePackageForSolutionDialogCommand = new OleMenuCommand(ShowManageLibraryPackageForSolutionDialog, null, BeforeQueryStatusForAddPackageForSolutionDialog, managePackageForSolutionDialogCommandID);
                mcs.AddCommand(managePackageForSolutionDialogCommand);

                // menu command for opening Package Source settings options page
                CommandID settingsCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, (int)PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                CommandID generalSettingsCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, (int)PkgCmdIDList.cmdIdGeneralSettings);
                OleMenuCommand generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                mcs.AddCommand(generalSettingsCommand);

                // menu command for Package Visualizer
                CommandID visualizerCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, (int)PkgCmdIDList.cmdIdVisualizer);
                OleMenuCommand visualizerCommand = new OleMenuCommand(ExecuteVisualizer, null, QueryStatusForVisualizer, visualizerCommandID);
                mcs.AddCommand(visualizerCommand);
            }
        }

        /// <summary>
        /// Gets whether the current IDE has an active, supported and non-unloaded project, which is a precondition for
        /// showing the Add Library Package Reference dialog
        /// </summary>
        private bool HasActiveLoadedSupportedProject {
            get {
                Project project = _vsMonitorSelection.GetActiveProject();
                return project != null && !project.IsUnloaded() && project.IsSupported();
            }
        }

        private bool IsSolutionOpen {
            get {
                return _dte != null && _dte.Solution != null && _dte.Solution.IsOpen;
            }
        }
    }
}