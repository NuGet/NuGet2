using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Dialog.ToolsOptionsUI;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Resources;
using NuGetConsole.Implementation;

namespace NuGet.Tools {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(PowerConsoleToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = "28836128-FC2C-11D2-A433-00C04F72D18A",
        Orientation = ToolWindowOrientation.Right)]
    [ProvideOptionPage(typeof(ToolsOptionsPage), "Package Manager", "General", 101, 102, true)]
    [ProvideProfile(typeof(ToolsOptionsPage), "Package Manager", "General", 101, 102, true)]
    [ProvideBindingPath] // Definition dll needs to be on VS binding path
    [Guid(GuidList.guidNuGetPkgString)]
    public sealed class NuGetPackage : Microsoft.VisualStudio.Shell.Package {
        public NuGetPackage() {
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
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowAddPackageDialog(object sender, EventArgs e) {
            if (HasActiveLoadedSupportedProject) {
                var window = new PackageManagerWindow(this);
                try {
                    window.ShowModal();
                }
                catch (TargetInvocationException exception) {
                    MessageBox.Show(
                        (exception.InnerException ?? exception).Message,
                        NuGet.Dialog.Resources.Dialog_MessageBoxTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else {
                Project project = DTEExtensions.DTE.GetActiveProject();
                string projectName = project != null ? project.Name : String.Empty;
                MessageBox.Show(
                    String.Format(CultureInfo.CurrentCulture, VsResources.DTE_ProjectUnsupported, projectName),
                    NuGet.Dialog.Resources.Dialog_MessageBoxTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args) {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = HasActiveLoadedSupportedProject;
        }

        private void ShowSettingsWindow(object sender, EventArgs args) {
            ShowOptionPage(typeof(NuGet.Dialog.ToolsOptionsUI.ToolsOptionsPage));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // set it here so that the rest of the extension can access the DTE
            DTEExtensions.DTE = (DTE)GetService(typeof(SDTE));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, (int)PkgCmdIDList.cmdidPowerConsole);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );

                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidNuGetDialogCmdSet, (int)PkgCmdIDList.cmdidAddPackageDialog);
                OleMenuCommand menuItem = new OleMenuCommand(ShowAddPackageDialog, null, BeforeQueryStatusForAddPackageDialog, menuCommandID);
                mcs.AddCommand(menuItem);

                CommandID settingsCommandID = new CommandID(GuidList.guidNuGetConsoleCmdSet, (int)PkgCmdIDList.cmdidSourceSettings);
                OleMenuCommand settingsMenuCommand = new OleMenuCommand(ShowSettingsWindow, settingsCommandID);
                mcs.AddCommand(settingsMenuCommand);
            }
        }

        /// <summary>
        /// Gets whether the current IDE has an active, supported and non-unloaded project, which is a precondition for
        /// showing the Add Library Package Reference dialog
        /// </summary>
        private bool HasActiveLoadedSupportedProject {
            get {
                Project project = DTEExtensions.DTE.GetActiveProject();
                return (project != null && !project.IsUnloaded() && project.IsSupported());
            }
        }
    }
}
