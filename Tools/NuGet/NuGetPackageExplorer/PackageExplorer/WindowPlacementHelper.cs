using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PackageExplorer {
    internal static class WindowPlacementHelper {

        public static void LoadWindowPlacementFromSettings(this Window window, string setting) {
            if (String.IsNullOrEmpty(setting)) {
                return;
            }

            WINDOWPLACEMENT wp = WINDOWPLACEMENT.Parse(setting);
            wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            wp.flags = 0;
            wp.showCmd = (wp.showCmd == NativeMethods.SW_SHOWMINIMIZED ? NativeMethods.SW_SHOWNORMAL : wp.showCmd);

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            NativeMethods.SetWindowPlacement(hwnd, ref wp);
        }

        public static string SaveWindowPlacementToSettings(this Window window) {
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            NativeMethods.GetWindowPlacement(hwnd, out wp);

            return wp.ToString();
        }
    }
}