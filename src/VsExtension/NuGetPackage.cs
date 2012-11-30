extern alias dialog;
extern alias dialog10;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Options;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGet.VisualStudio11;
using NuGetConsole;
using NuGetConsole.Implementation;
using ManagePackageDialog = dialog::NuGet.Dialog.PackageManagerWindow;
using VS10ManagePackageDialog = dialog10::NuGet.Dialog.PackageManagerWindow;

namespace NuGet.Tools
{
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
    [ProvideSearchProvider(typeof(NuGetSearchProvider), "NuGet Search")]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [ProvideAutoLoad(GuidList.guidAutoLoadNuGetString)]
    [FontAndColorsRegistration(
        "Package Manager Console",
        NuGetConsole.Implementation.GuidList.GuidPackageManagerConsoleFontAndColorCategoryString,
        "{" + GuidList.guidNuGetPkgString + "}")]
    [Guid(GuidList.guidNuGetPkgString)]
    public sealed class NuGetPackage : Package, IVsPackageExtensionProvider
    {
        // This product version will be updated by the build script to match the daily build version.
        // It is displayed in the Help - About box of Visual Studio
        public const string ProductVersion = "2.2.0.0";
        private static readonly string[] _visualizerSupportedSKUs = new[] { "Premium", "Ultimate" };

        private uint _solutionNotBuildingAndNotDebuggingContextCookie;
        private DTE _dte;
        private IConsoleStatus _consoleStatus;
        private IVsMonitorSelection _vsMonitorSelection;
        private bool? _isVisualizerSupported;
        private IPackageRestoreManager _packageRestoreManager;
        private ISolutionManager _solutionManager;
        private IDeleteOnRestartManager _deleteOnRestart;
        private OleMenuCommand _managePackageDialogCommand;
        private OleMenuCommand _managePackageForSolutionDialogCommand;
        private OleMenuCommandService _mcs;

        public NuGetPackage()
        {
            ServiceLocator.InitializePackageServiceProvider(this);
        }

        private IVsMonitorSelection VsMonitorSelection
        {
            get
            {
                if (_vsMonitorSelection == null)
                {
                    // get the UI context cookie for the debugging mode
                    _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));

                    // get the solution not building and not debugging cookie
                    Guid guid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
                    _vsMonitorSelection.GetCmdUIContextCookie(ref guid, out _solutionNotBuildingAndNotDebuggingContextCookie);
                }
                return _vsMonitorSelection;
            }
        }

        private IConsoleStatus ConsoleStatus
        {
            get
            {
                if (_consoleStatus == null)
                {
                    _consoleStatus = ServiceLocator.GetInstance<IConsoleStatus>();
                    Debug.Assert(_consoleStatus != null);
                }

                return _consoleStatus;
            }
        }

        private IPackageRestoreManager PackageRestoreManager
        {
            get
            {
                if (_packageRestoreManager == null)
                {
                    _packageRestoreManager = ServiceLocator.GetInstance<IPackageRestoreManager>();
                    Debug.Assert(_packageRestoreManager != null);
                }
                return _packageRestoreManager;
            }
        }

        private ISolutionManager SolutionManager
        {
            get
            {
                if (_solutionManager == null)
                {
                    _solutionManager = ServiceLocator.GetInstance<ISolutionManager>();
                    Debug.Assert(_solutionManager != null);
                }
                return _solutionManager;
            }
        }

        private IDeleteOnRestartManager DeleteOnRestart
        {
            get
            {
                if (_deleteOnRestart == null)
                {
                    _deleteOnRestart = ServiceLocator.GetInstance<IDeleteOnRestartManager>();
                    Debug.Assert(_deleteOnRestart != null);
                }

                return _deleteOnRestart;
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            AddMenuCommandHandlers();

            // IMPORTANT: Do NOT do anything that can lead to a call to ServiceLocator.GetGlobalService(). 
            // Doing so is illegal and may cause VS to hang.

            _dte = (DTE)GetService(typeof(SDTE));
            Debug.Assert(_dte != null);

            // set default credential provider for the HttpClient
            var webProxy = (IVsWebProxy)GetService(typeof(SVsWebProxy));
            Debug.Assert(webProxy != null);

            var settings = Settings.LoadDefaultSettings(_solutionManager == null ? null : _solutionManager.SolutionFileSystem);
            var packageSourceProvider = new PackageSourceProvider(settings);
            HttpClient.DefaultCredentialProvider = new SettingsCredentialProvider(new VSRequestCredentialProvider(webProxy), packageSourceProvider);

            // when NuGet loads, if the current solution has package 
            // restore mode enabled, we make sure every thing is set up correctly.
            // For example, projects which were added outside of VS need to have
            // the <Import> element added.
            if (PackageRestoreManager.IsCurrentSolutionEnabledForRestore)
            {
                PackageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: false);
            }

            // when NuGet loads, if the current solution has some package 
            // folders marked for deletion (because a previous uninstalltion didn't succeed),
            // delete them now.
            if (SolutionManager.IsSolutionOpen)
            {
                DeleteOnRestart.DeleteMarkedPackageDirectories();
            }
        }

        private void AddMenuCommandHandlers()
        {
            _mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != _mcs)
            {
                // menu command for opening Package Manager Console
                CommandID toolwndCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, PkgCmdIDList.cmdidPowerConsole);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                _mcs.AddCommand(menuToolWin);

                // menu command for opening Manage NuGet packages dialog
                CommandID managePackageDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialog);
                _managePackageDialogCommand = new OleMenuCommand(ShowManageLibraryPackageDialog, null, BeforeQueryStatusForAddPackageDialog, managePackageDialogCommandID);
                // ‘$’ - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                _managePackageDialogCommand.ParametersDescription = "$";
                _mcs.AddCommand(_managePackageDialogCommand);

                // menu command for opening "Manage NuGet packages for solution" dialog
                CommandID managePackageForSolutionDialogCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, PkgCmdIDList.cmdidAddPackageDialogForSolution);
                _managePackageForSolutionDialogCommand = new OleMenuCommand(ShowManageLibraryPackageForSolutionDialog, null, BeforeQueryStatusForAddPackageForSolutionDialog, managePackageForSolutionDialogCommandID);
                // ‘$’ - This indicates that the input line other than the argument forms a single argument string with no autocompletion
                //       Autocompletion for filename(s) is supported for option 'p' or 'd' which is not applicable for this command
                _managePackageForSolutionDialogCommand.ParametersDescription = "$";
                _mcs.AddCommand(_managePackageForSolutionDialogCommand);

                // menu command for opening Package Source settings options page
                CommandID settingsCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowPackageSourcesOptionPage, settingsCommandID);
                _mcs.AddCommand(settingsMenuCommand);

                // menu command for opening General options page
                CommandID generalSettingsCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, PkgCmdIDList.cmdIdGeneralSettings);
                OleMenuCommand generalSettingsCommand = new OleMenuCommand(ShowGeneralSettingsOptionPage, generalSettingsCommandID);
                _mcs.AddCommand(generalSettingsCommand);

                // menu command for Package Visualizer
                CommandID visualizerCommandID = new CommandID(GuidList.guidNuGetToolsGroupCmdSet, PkgCmdIDList.cmdIdVisualizer);
                OleMenuCommand visualizerCommand = new OleMenuCommand(ExecuteVisualizer, null, QueryStatusForVisualizer, visualizerCommandID);
                _mcs.AddCommand(visualizerCommand);

                // menu command for Package Restore command
                CommandID restorePackagesCommandID = new CommandID(GuidList.guidNuGetPackagesRestoreCmdSet, PkgCmdIDList.cmdidRestorePackages);
                var restorePackagesCommand = new OleMenuCommand(EnablePackagesRestore, null, QueryStatusEnablePackagesRestore, restorePackagesCommandID);
                _mcs.AddCommand(restorePackagesCommand);
            }
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(PowerConsoleToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// Executes the NuGet Visualizer.
        /// </summary>
        private void ExecuteVisualizer(object sender, EventArgs e)
        {
            var visualizer = new NuGet.Dialog.Visualizer(
                ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
                ServiceLocator.GetInstance<ISolutionManager>());
            string outputFile = visualizer.CreateGraph();
            _dte.ItemOperations.OpenFile(outputFile);
        }

        private void ShowManageLibraryPackageDialog(object sender, EventArgs e)
        {
            string parameterString = null;
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (null != args)
            {
                parameterString = args.InValue as string;
            }

            if (VsMonitorSelection.GetIsSolutionNodeSelected())
            {
                ShowManageLibraryPackageDialog(null, parameterString);
            }
            else
            {
                Project project = VsMonitorSelection.GetActiveProject();
                if (project != null && !project.IsUnloaded() && project.IsSupported())
                {
                    ShowManageLibraryPackageDialog(project, parameterString);
                }
                else
                {
                    // show error message when no supported project is selected.
                    string projectName = project != null ? project.Name : String.Empty;

                    string errorMessage = String.IsNullOrEmpty(projectName) 
                        ? Resources.NoProjectSelected 
                        : String.Format(CultureInfo.CurrentCulture, VsResources.DTE_ProjectUnsupported, projectName);

                    MessageHelper.ShowWarningMessage(errorMessage, Resources.ErrorDialogBoxTitle);
                }
            }
        }

        private void ShowManageLibraryPackageForSolutionDialog(object sender, EventArgs e)
        {
            string parameterString = null;
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null)
            {
                parameterString = args.InValue as string;
            }
            ShowManageLibraryPackageDialog(null, parameterString);
        }

        private static void ShowManageLibraryPackageDialog(Project project, string parameterString = null)
        {
            DialogWindow window = VsVersionHelper.IsVisualStudio2010 ?
                GetVS10PackageManagerWindow(project, parameterString) :
                GetPackageManagerWindow(project, parameterString);
            try
            {
                window.ShowModal();
            }
            catch (TargetInvocationException exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVS10PackageManagerWindow(Project project, string parameterString)
        {
            return new VS10ManagePackageDialog(project, parameterString);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetPackageManagerWindow(Project project, string parameterString)
        {
            return new ManagePackageDialog(project, parameterString);
        }

        private void EnablePackagesRestore(object sender, EventArgs args)
        {
            _packageRestoreManager.EnableCurrentSolutionForRestore(fromActivation: true);
        }

        private void QueryStatusEnablePackagesRestore(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = SolutionManager.IsSolutionOpen && !PackageRestoreManager.IsCurrentSolutionEnabledForRestore;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding() && HasActiveLoadedSupportedProject;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void BeforeQueryStatusForAddPackageForSolutionDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding();
            // disable the dialog menu if the console is busy executing a command;
            command.Enabled = !ConsoleStatus.IsBusy;
        }

        private void QueryStatusForVisualizer(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = SolutionManager.IsSolutionOpen && IsVisualizerSupported;
        }

        private bool IsSolutionExistsAndNotDebuggingAndNotBuilding()
        {
            int pfActive;
            int result = VsMonitorSelection.IsCmdUIContextActive(_solutionNotBuildingAndNotDebuggingContextCookie, out pfActive);
            return (result == VSConstants.S_OK && pfActive > 0);
        }

        private void ShowPackageSourcesOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(PackageSourceOptionsPage));
        }

        private void ShowGeneralSettingsOptionPage(object sender, EventArgs args)
        {
            ShowOptionPageSafe(typeof(GeneralOptionPage));
        }

        private void ShowOptionPageSafe(Type optionPageType)
        {
            try
            {
                ShowOptionPage(optionPageType);
            }
            catch (Exception exception)
            {
                MessageHelper.ShowErrorMessage(exception, Resources.ErrorDialogBoxTitle);
                ExceptionHelper.WriteToActivityLog(exception);
            }
        }

        /// <summary>
        /// Gets whether the current IDE has an active, supported and non-unloaded project, which is a precondition for
        /// showing the Add Library Package Reference dialog
        /// </summary>
        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                Project project = VsMonitorSelection.GetActiveProject();
                return project != null && !project.IsUnloaded() && project.IsSupported();
            }
        }

        private bool IsVisualizerSupported
        {
            get
            {
                if (_isVisualizerSupported == null)
                {
                    _isVisualizerSupported = _visualizerSupportedSKUs.Contains(_dte.Edition, StringComparer.OrdinalIgnoreCase);
                }
                return _isVisualizerSupported.Value;
            }
        }

        public dynamic CreateExtensionInstance(ref Guid extensionPoint, ref Guid instance)
        {
            if (instance == typeof(NuGetSearchProvider).GUID)
            {
                return new NuGetSearchProvider(_mcs, _managePackageDialogCommand, _managePackageForSolutionDialogCommand);
            }

            return null;
        }
    }
}