using System;
using System.Runtime.InteropServices;

namespace NuGet.Dialog
{
    internal static class NativeMethods
    {
        public const int MF_BYPOSITION = 0x400;

        [DllImport("User32")]
        public static extern int RemoveMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("User32")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, [param: MarshalAs(UnmanagedType.Bool)] bool bRevert);

        [DllImport("User32")]
        public static extern int GetMenuItemCount(IntPtr hWnd);
    }
}