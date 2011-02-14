using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PackageExplorer {
    internal static class NativeMethods {

        public static readonly int GWL_EXSTYLE =(-20);

        public static readonly int WS_EX_DLGMODALFRAME = 0x00000001;

        public static readonly int WS_EX_CONTEXTHELP = 0x00000400;

        [DllImport("user32.dll", EntryPoint="GetWindowLong")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);


        [DllImport("user32.dll", EntryPoint="SetWindowLong")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}
