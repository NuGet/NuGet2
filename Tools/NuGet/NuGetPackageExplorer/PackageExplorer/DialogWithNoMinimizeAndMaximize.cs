using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace PackageExplorer
{
    public class DialogWithNoMinimizeAndMaximize : Window
    {

        [DllImport("user32.dll")]
        internal extern static int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        internal extern static int GetWindowLong(IntPtr hwnd, int index);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            const int GWL_STYLE = -16; 
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle; 
            long value = GetWindowLong(hwnd, GWL_STYLE); 
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & -131073 & -65537));
        }
    }
}
