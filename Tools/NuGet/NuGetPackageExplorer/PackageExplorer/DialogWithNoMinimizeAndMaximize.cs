using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace PackageExplorer
{
    public class DialogWithNoMinimizeAndMaximize : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            const int GWL_STYLE = -16; 
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle; 
            long value = NativeMethods.GetWindowLong(hwnd, GWL_STYLE); 
            NativeMethods.SetWindowLong(hwnd, GWL_STYLE, (int)(value & -131073 & -65537));
        }
    }
}
