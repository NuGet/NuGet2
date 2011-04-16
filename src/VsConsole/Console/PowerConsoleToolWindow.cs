using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using NuGetConsole.Implementation.Console;
using NuGetConsole.Implementation.PowerConsole;
using NuGet.VisualStudio;

namespace NuGetConsole.Implementation {
    /// <summary>
    /// This class implements the tool window.
    /// </summary>
    [Guid("0AD07096-BBA9-4900-A651-0598D26F6D24")]
    public sealed class PowerConsoleToolWindow : ToolWindowPane, IOleCommandTarget {
        /// <summary>
        /// Get VS IComponentModel service.
        /// </summary>
        private IComponentModel ComponentModel {
            get {
                return this.GetService<IComponentModel>(typeof(SComponentModel));
            }
        }

        private IProductUpdateService ProductUpdateService {
            get {
                return ComponentModel.GetService<IProductUpdateService>();
            }
        }

        /// <summary>
        /// Get IWpfConsoleService through MEF.
        /// </summary>
        private IWpfConsoleService WpfConsoleService {
            get {
                return ComponentModel.GetService<IWpfConsoleService>();
            }
        }

        private PowerConsoleWindow PowerConsoleWindow {
            get {
                return ComponentModel.GetService<IPowerConsoleWindow>() as PowerConsoleWindow;
            }
        }

        private IVsUIShell VsUIShell {
            get {
                return this.GetService<IVsUIShell>(typeof(SVsUIShell));
            }
        }

        private bool IsToolbarEnabled {
            get {
                return _wpfConsole != null &&
                       _wpfConsole.Dispatcher.IsStartCompleted &&
                       _wpfConsole.Host != null &&
                       _wpfConsole.Host.IsCommandEnabled;
            }
        }

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public PowerConsoleToolWindow() :
            base(null) {
            this.Caption = Resources.ToolWindowTitle;
            this.BitmapResourceID = 301;
            this.BitmapIndex = 0;
            this.ToolBar = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.idToolbar);
        }

        protected override void Initialize() {
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null) {
                // Get list command for the Feed combo
                CommandID sourcesListCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidSourcesList);
                mcs.AddCommand(new OleMenuCommand(SourcesList_Exec, sourcesListCommandID));

                // invoke command for the Feed combo
                CommandID sourcesCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidSources);
                mcs.AddCommand(new OleMenuCommand(Sources_Exec, sourcesCommandID));

                // get default project command
                CommandID projectsListCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidProjectsList);
                mcs.AddCommand(new OleMenuCommand(ProjectsList_Exec, projectsListCommandID));

                // invoke command for the Default project combo
                CommandID projectsCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidProjects);
                mcs.AddCommand(new OleMenuCommand(Projects_Exec, projectsCommandID));

                // clear console command
                CommandID clearHostCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidClearHost);
                mcs.AddCommand(new OleMenuCommand(ClearHost_Exec, clearHostCommandID));

                // terminate command execution command
                CommandID stopHostCommandID = new CommandID(GuidList.guidNuGetCmdSet, PkgCmdIDList.cmdidStopHost);
                mcs.AddCommand(new OleMenuCommand(StopHost_Exec, stopHostCommandID));
            }
        }

        public override void OnToolWindowCreated() {
            // Register key bindings to use in the editor
            var windowFrame = (IVsWindowFrame)Frame;
            Guid cmdUi = VSConstants.GUID_TextEditorFactory;
            windowFrame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref cmdUi);

            // pause for a tiny moment to let the tool window open before initializing the host
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(0);
            timer.Tick += (o, e) => {
                timer.Stop();
                LoadConsoleEditor();
            };
            timer.Start();

            base.OnToolWindowCreated();
        }

        /// <summary>
        /// This override allows us to forward these messages to the editor instance as well
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected override bool PreProcessMessage(ref System.Windows.Forms.Message m) {
            IVsWindowPane vsWindowPane = this.VsTextView as IVsWindowPane;
            if (vsWindowPane != null) {
                MSG[] pMsg = new MSG[1];
                pMsg[0].hwnd = m.HWnd;
                pMsg[0].message = (uint)m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;

                return vsWindowPane.TranslateAccelerator(pMsg) == 0;
            }

            return base.PreProcessMessage(ref m);
        }

        /// <summary>
        /// Override to forward to editor or handle accordingly if supported by this tool window.
        /// </summary>
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {

            // examine buttons within our toolbar
            if (pguidCmdGroup == GuidList.guidNuGetCmdSet) {
                bool isEnabled = IsToolbarEnabled;

                if (isEnabled) {
                    bool isStopButton = (prgCmds[0].cmdID == 0x0600);   // 0x0600 is the Command ID of the Stop button, defined in .vsct

                    // when command is executing: enable stop button and disable the rest
                    // when command is not executing: disable the stop button and enable the rest
                    isEnabled = !isStopButton ^ WpfConsole.Dispatcher.IsExecutingCommand;
                }

                if (isEnabled) {
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                }
                else {
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED);
                }

                return VSConstants.S_OK;
            }

            int hr = OleCommandFilter.OLECMDERR_E_NOTSUPPORTED;

            if (this.VsTextView != null) {
                IOleCommandTarget cmdTarget = (IOleCommandTarget)VsTextView;
                hr = cmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            if (hr == OleCommandFilter.OLECMDERR_E_NOTSUPPORTED) {
                IOleCommandTarget target = this.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
                if (target != null) {
                    hr = target.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            return hr;
        }

        /// <summary>
        /// Override to forward to editor or handle accordingly if supported by this tool window.
        /// </summary>
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            int hr = OleCommandFilter.OLECMDERR_E_NOTSUPPORTED;

            if (this.VsTextView != null) {
                IOleCommandTarget cmdTarget = (IOleCommandTarget)VsTextView;
                hr = cmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            if (hr == OleCommandFilter.OLECMDERR_E_NOTSUPPORTED) {
                IOleCommandTarget target = this.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
                if (target != null) {
                    hr = target.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            return hr;
        }

        private void SourcesList_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null || args.OutValue == IntPtr.Zero) {
                    throw new ArgumentException("Invalid argument", "e");
                }
                Marshal.GetNativeVariantForObject(PowerConsoleWindow.PackageSources, args.OutValue);
            }
        }

        /// <summary>
        /// Called to retrieve current combo item name or to select a new item.
        /// </summary>
        private void Sources_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null && args.InValue is int) // Selected a feed
                {
                    int index = (int)args.InValue;
                    if (index >= 0 && index < PowerConsoleWindow.PackageSources.Length) {
                        PowerConsoleWindow.ActivePackageSource = PowerConsoleWindow.PackageSources[index];
                    }
                }
                else if (args.OutValue != IntPtr.Zero) // Query selected feed name
                {
                    string displayName = PowerConsoleWindow.ActivePackageSource ?? string.Empty;
                    Marshal.GetNativeVariantForObject(displayName, args.OutValue);
                }
            }
        }

        private void ProjectsList_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null || args.OutValue == IntPtr.Zero) {
                    throw new ArgumentException("Invalid argument", "e");
                }

                // get project list here
                Marshal.GetNativeVariantForObject(PowerConsoleWindow.AvailableProjects, args.OutValue);
            }
        }

        /// <summary>
        /// Called to retrieve current combo item name or to select a new item.
        /// </summary>
        private void Projects_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null && args.InValue is int) // Selected a default projects
                {
                    int index = (int)args.InValue;
                    if (index >= 0 && index < PowerConsoleWindow.AvailableProjects.Length) {
                        PowerConsoleWindow.DefaultProject = PowerConsoleWindow.AvailableProjects[index];
                    }
                }
                else if (args.OutValue != IntPtr.Zero) // Query default project name
                {
                    string displayName = PowerConsoleWindow.DefaultProject ?? string.Empty;
                    Marshal.GetNativeVariantForObject(displayName, args.OutValue);
                }
            }
        }

        /// <summary>
        /// ClearHost command handler.
        /// </summary>
        private void ClearHost_Exec(object sender, EventArgs e) {
            if (WpfConsole != null) {
                WpfConsole.Dispatcher.ClearConsole();
            }
        }

        private void StopHost_Exec(object sender, EventArgs e) {
            if (WpfConsole != null) {
                WpfConsole.Host.Abort();
            }
        }

        private HostInfo ActiveHostInfo {
            get {
                return PowerConsoleWindow.ActiveHostInfo;
            }
        }

        private void LoadConsoleEditor() {
            if (WpfConsole != null) {
                FrameworkElement consolePane = WpfConsole.Content as FrameworkElement;
                ConsoleParentPane.AddConsoleEditor(consolePane);

                // WPF doesn't handle input focus automatically in this scenario. We
                // have to set the focus manually, otherwise the editor is displayed but
                // not focused and not receiving keyboard inputs until clicked.
                if (consolePane != null) {
                    PendingMoveFocus(consolePane);
                }
            }
        }

        /// <summary>
        /// Set pending focus to a console pane. At the time of setting active host,
        /// the pane (UIElement) is usually not loaded yet and can't receive focus.
        /// In this case, we need to set focus in its Loaded event.
        /// </summary>
        /// <param name="consolePane"></param>
        private void PendingMoveFocus(FrameworkElement consolePane) {
            if (consolePane.IsLoaded && consolePane.IsConnectedToPresentationSource()) {
                PendingFocusPane = null;
                MoveFocus(consolePane);
            }
            else {
                PendingFocusPane = consolePane;
            }
        }

        private FrameworkElement _pendingFocusPane;
        private FrameworkElement PendingFocusPane {
            get {
                return _pendingFocusPane;
            }
            set {
                if (_pendingFocusPane != null) {
                    _pendingFocusPane.Loaded -= PendingFocusPane_Loaded;
                }
                _pendingFocusPane = value;
                if (_pendingFocusPane != null) {
                    _pendingFocusPane.Loaded += PendingFocusPane_Loaded;
                }
            }
        }

        private void PendingFocusPane_Loaded(object sender, RoutedEventArgs e) {
            MoveFocus(PendingFocusPane);
            PendingFocusPane = null;
        }

        private void MoveFocus(FrameworkElement consolePane) {
            // TAB focus into editor (consolePane.Focus() does not work due to editor layouts)
            consolePane.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            // Try start the console session now. This needs to be after the console
            // pane getting focus to avoid incorrect initial editor layout.
            StartConsoleSession(consolePane);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We really don't want exceptions from the console to bring down VS")]
        private void StartConsoleSession(FrameworkElement consolePane) {
            if (WpfConsole != null && WpfConsole.Content == consolePane) {
                if (WpfConsole.Host.IsCommandEnabled) {
                    try {
                        if (WpfConsole.Dispatcher.IsStartCompleted) {
                            OnDispatcherStartCompleted();
                        }
                        else {
                            WpfConsole.Dispatcher.StartCompleted += (sender, args) => {
                                OnDispatcherStartCompleted();
                            };
                            WpfConsole.Dispatcher.Start();
                        }
                    }
                    catch (Exception x) {
                        WpfConsole.WriteLine(x.ToString());
                    }
                }
            }
        }

        private void OnDispatcherStartCompleted() {
            ConsoleParentPane.NotifyInitializationCompleted();

            // force the UI to update the toolbar
            VsUIShell.UpdateCommandUI(0 /* false = update UI asynchronously */);
        }

        private IWpfConsole _wpfConsole;

        /// <summary>
        /// Get the WpfConsole of the active host.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IWpfConsole WpfConsole {
            get {
                if (_wpfConsole == null) {
                    Debug.Assert(ActiveHostInfo != null);

                    try {
                        _wpfConsole = ActiveHostInfo.WpfConsole;
                    }
                    catch (Exception x) {
                        _wpfConsole = ActiveHostInfo.WpfConsole;
                        _wpfConsole.Write(x.ToString());
                    }
                }

                return _wpfConsole;
            }
        }

        private IVsTextView _vsTextView;

        /// <summary>
        /// Get the VsTextView of current WpfConsole if exists.
        /// </summary>
        private IVsTextView VsTextView {
            get {
                if (_vsTextView == null && _wpfConsole != null) {
                    _vsTextView = (IVsTextView)(WpfConsole.VsTextView);
                }
                return _vsTextView;
            }
        }

        private ConsoleContainer _consoleParentPane;

        /// <summary>
        /// Get the parent pane of console panes. This serves as the Content of this tool window.
        /// </summary>
        private ConsoleContainer ConsoleParentPane {
            get {
                if (_consoleParentPane == null) {
                    _consoleParentPane = new ConsoleContainer(ProductUpdateService);
                }
                return _consoleParentPane;
            }
        }

        public override object Content {
            get {
                return this.ConsoleParentPane;
            }
            set {
                base.Content = value;
            }
        }
    }
}