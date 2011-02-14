using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PackageExplorer
{
    /// <summary>
    /// A class that encapsulates the functionality for a Dialog window. It derives
    /// from the base WPF window and customizes the appearance to look like a typical
    /// Windows dialog.
    /// </summary>
    public class DialogWindow : Window
    {
        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DialogWindow()
        {
        }
        #endregion

        #region DependencyProperties
        public static readonly DependencyProperty IsSystemMenuVisibleProperty =
            DependencyProperty.Register("IsSystemMenuVisible", typeof(bool), typeof(DialogWindow),
                                        new FrameworkPropertyMetadata(false, OnPropertyChanged));

        public static readonly DependencyProperty IsHelpButtonVisibleProperty =
            DependencyProperty.Register("IsHelpButtonVisible", typeof(bool), typeof(DialogWindow),
                                        new FrameworkPropertyMetadata(false, OnPropertyChanged));

        /// <summary>
        /// Gets or sets whether the dialog's system menu should be visible.
        /// For a dialog, the default is no system menu.
        /// </summary>
        public bool IsSystemMenuVisible
        {
            get { return (bool)GetValue(IsSystemMenuVisibleProperty); }
            set { SetValue(IsSystemMenuVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the dialog's context help button should be visible.
        /// For a dialog, the default is false.
        /// </summary>
        public bool IsHelpButtonVisible
        {
            get { return (bool)GetValue(IsHelpButtonVisibleProperty); }
            set { SetValue(IsHelpButtonVisibleProperty, value); }
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Overrides the base source initialization and sets the appropriate
        /// window styles for dialogs.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetWindowStyle();
        }

        /// <summary>
        /// Handles when one of DialogWindow's dependency properties has changed.
        /// </summary>
        /// <param name="obj">Object instance on which the property changed.</param>
        /// <param name="e">Event args</param>
        static void OnPropertyChanged(object obj, DependencyPropertyChangedEventArgs e)
        {
            DialogWindow dialog = (DialogWindow)obj;
            dialog.SetWindowStyle();
        }
        #endregion

        #region Helper method
        /// <summary>
        /// Sets the appropriate window style for the dialog window based on the
        /// IsSystemMenuVisible and IsHelpButtonVisible properties.
        /// 
        /// Note: this makes several calls into native Windows methods to deliver
        /// this functionality. NativeMethods is a wrapper for native Windows 
        /// calls.
        /// </summary>
        protected virtual void SetWindowStyle()
        {
            // Gets a window handle for this dialog window.
            WindowInteropHelper wih = new WindowInteropHelper(this);
            IntPtr hwnd = wih.Handle;

            // Gets the current windows StyleEx value.
            int windowStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt32();

            // Turns modal dialog frame on/off depending on whether we want to show 
            // the system menu.
            if (IsSystemMenuVisible)
            {
                windowStyle &= ~NativeMethods.WS_EX_DLGMODALFRAME;
            }
            else
            {
                windowStyle |= NativeMethods.WS_EX_DLGMODALFRAME;
            }

            // Turns context help on/off for the dialog depending is we want it shown.
            if (IsHelpButtonVisible)
            {
                windowStyle |= NativeMethods.WS_EX_CONTEXTHELP;
            }
            else
            {
                windowStyle &= ~NativeMethods.WS_EX_CONTEXTHELP;
            }

            // Now, sets the new windows StyleEx value.
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(windowStyle));

            //if (!IsSystemMenuVisible && this.ResizeMode == ResizeMode.NoResize)
            //{
            //    // Note: this is a workaround for a WPF bug. When NoResize is chosen,
            //    // the system menu doesn't get set up correctly. The code below disables
            //    // the appropriate system menu items in this case.
            //    IntPtr hmenu = NativeMethods.GetSystemMenu(hwnd, false);
            //    NativeMethods.EnableMenuItem(hmenu, NativeMethods.SysMenuPos_Maximize,
            //                                 NativeMethods.MF_DISABLE | NativeMethods.MF_BYPOSITION);
            //    NativeMethods.EnableMenuItem(hmenu, NativeMethods.SysMenuPos_Minimize,
            //                                 NativeMethods.MF_DISABLE | NativeMethods.MF_BYPOSITION);
            //    NativeMethods.EnableMenuItem(hmenu, NativeMethods.SysMenuPos_Size,
            //                                 NativeMethods.MF_DISABLE | NativeMethods.MF_BYPOSITION);
            //    NativeMethods.EnableMenuItem(hmenu, NativeMethods.SysMenuPos_Restore,
            //                                 NativeMethods.MF_DISABLE | NativeMethods.MF_BYPOSITION);

            //    NativeMethods.DrawMenuBar(hwnd);
            //}
        }
        #endregion
    }
}
