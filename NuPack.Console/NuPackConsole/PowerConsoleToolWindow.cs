using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using NuGetConsole.Implementation.Console;
using NuGetConsole.Implementation.PowerConsole;

namespace NuGetConsole.Implementation {
    /// <summary>
    /// This class implements the tool window.
    /// </summary>
    [Guid("0AD07096-BBA9-4900-A651-0598D26F6D24")]
    public sealed class PowerConsoleToolWindow : ToolWindowPane, IOleCommandTarget {
        /// <summary>
        /// Get VS IComponentModel service.
        /// </summary>
        IComponentModel ComponentModel {
            get {
                return this.GetService<IComponentModel>(typeof(SComponentModel));
            }
        }

        /// <summary>
        /// Get IWpfConsoleService through MEF.
        /// </summary>
        IWpfConsoleService WpfConsoleService {
            get {
                return ComponentModel.GetService<IWpfConsoleService>();
            }
        }

        PowerConsoleWindow PowerConsoleWindow {
            get {
                return ComponentModel.GetService<IPowerConsoleWindow>() as PowerConsoleWindow;
            }
        }

        bool IsToolbarEnabled {
            get {
                return (WpfConsole != null && WpfConsole.Host != null && WpfConsole.Host.IsCommandEnabled);
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
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override void OnClose() {
            // Save ActiveHost on close, ignore errors.
            try {
                Settings.SetDefaultHost(this, PowerConsoleWindow.ActiveHost);
            }
            catch (Exception x) {
                Trace.TraceError(x.ToString());
            }

            base.OnClose();
        }

        public override void OnToolWindowCreated() {
            // Register key bindings to use in the editor
            var windowFrame = (IVsWindowFrame)Frame;
            Guid cmdUi = VSConstants.GUID_TextEditorFactory;
            windowFrame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref cmdUi);

            PowerConsoleWindow.ActiveHostChanged += PowerConsoleWindow_ActiveHostChanged;
            PowerConsoleWindow_ActiveHostChanged(PowerConsoleWindow, null);

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
            
            if (!IsToolbarEnabled) {
                // disbale all buttons on the toolbar
                if (pguidCmdGroup == GuidList.guidNuGetCmdSet) {
                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                    return VSConstants.S_OK;
                }
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

        void SourcesList_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null || args.OutValue == IntPtr.Zero) {
                    throw new ArgumentException("Invalid argument", "e");
                }
                Marshal.GetNativeVariantForObject(PowerConsoleWindow.AvailableHostSettings, args.OutValue);
            }
        }

        /// <summary>
        /// Called to retrieve current combo item name or to select a new item.
        /// </summary>
        void Sources_Exec(object sender, EventArgs e) {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null) {
                if (args.InValue != null && args.InValue is int) // Selected a feed
                {
                    int index = (int)args.InValue;
                    if (index >= 0 && index < PowerConsoleWindow.AvailableHostSettings.Length) {
                        PowerConsoleWindow.ActiveHostSetting = PowerConsoleWindow.AvailableHostSettings[index];
                    }
                }
                else if (args.OutValue != IntPtr.Zero) // Query selected feed name
                {
                    string displayName = PowerConsoleWindow.ActiveHostSetting ?? string.Empty;
                    Marshal.GetNativeVariantForObject(displayName, args.OutValue);
                }
            }
        }

        void ProjectsList_Exec(object sender, EventArgs e) {
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
        void Projects_Exec(object sender, EventArgs e) {
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
        void ClearHost_Exec(object sender, EventArgs e) {
            if (WpfConsole != null) {
                WpfConsole.Dispatcher.ClearConsole();
            }
        }

        HostInfo ActiveHostInfo {
            get {
                return PowerConsoleWindow.ActiveHostInfo;
            }
        }

        void PowerConsoleWindow_ActiveHostChanged(object sender, EventArgs e) {
            // Reset local caching variables
            _wpfConsole = null;
            _vsTextView = null;

            // Switch to new WpfConsole if available
            if (WpfConsole != null) {
                FrameworkElement consolePane = WpfConsole.Content as FrameworkElement;
                ConsoleParentPane.Child = consolePane;

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
        void PendingMoveFocus(FrameworkElement consolePane) {
            if (consolePane.IsLoaded && consolePane.IsConnectedToPresentationSource()) {
                PendingFocusPane = null;
                MoveFocus(consolePane);
            }
            else {
                PendingFocusPane = consolePane;
            }
        }

        FrameworkElement _pendingFocusPane;
        FrameworkElement PendingFocusPane {
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

        void PendingFocusPane_Loaded(object sender, RoutedEventArgs e) {
            MoveFocus(PendingFocusPane);
            PendingFocusPane = null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We really don't want exceptions from the console to bring down VS")]
        void MoveFocus(FrameworkElement consolePane) {
            // TAB focus into editor (consolePane.Focus() does not work due to editor layouts)
            consolePane.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            // Try start the console session now. This needs to be after the console
            // pane getting focus to avoid incorrect initial editor layout.
            if (WpfConsole != null && WpfConsole.Content == consolePane) {

                if (WpfConsole.Host.IsCommandEnabled) {
                    try {
                        WpfConsole.Dispatcher.Start();
                    }
                    catch (Exception x) {
                        WpfConsole.WriteLine(x.ToString());
                    }
                }
            }
        }

        IWpfConsole _wpfConsole;

        /// <summary>
        /// Get the WpfConsole of the active host.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        IWpfConsole WpfConsole {
            get {
                if (_wpfConsole == null) {
                    if (ActiveHostInfo != null) {
                        try {
                            _wpfConsole = ActiveHostInfo.WpfConsole;
                        }
                        catch (Exception x) {
                            _wpfConsole = ActiveHostInfo.WpfConsole;
                            _wpfConsole.Write(x.ToString());
                        }
                    }
                    else {
                        // TODO: no host
                    }
                }

                return _wpfConsole;
            }
        }

        IVsTextView _vsTextView;

        /// <summary>
        /// Get the VsTextView of current WpfConsole if exists.
        /// </summary>
        IVsTextView VsTextView {
            get {
                if (_vsTextView == null && WpfConsole != null) {
                    _vsTextView = (IVsTextView)(WpfConsole.VsTextView);
                }
                return _vsTextView;
            }
        }

        Border _consoleParentPane;

        /// <summary>
        /// Get the parent pane of console panes. This serves as the Content of this tool window.
        /// </summary>
        Border ConsoleParentPane {
            get {
                if (_consoleParentPane == null) {
                    _consoleParentPane = new Border();
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
