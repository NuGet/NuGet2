using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PackageExplorer {

    public class BrowseForFolder {

        private string _initialPath;

        public int OnBrowseEvent(IntPtr hwnd, int msg, IntPtr lp, IntPtr lpData) {
            switch (msg) {
                case NativeMethods.BFFM_INITIALIZED: // Required to set initialPath
                {
                    //Win32.SendMessage(new HandleRef(null, hWnd), BFFM_SETSELECTIONA, 1, lpData);
                    // Use BFFM_SETSELECTIONW if passing a Unicode string, i.e. native CLR Strings.
                    NativeMethods.SendMessage(new HandleRef(null, hwnd), NativeMethods.BFFM_SETSELECTIONW, 1, _initialPath);
                    break;
                }
                case NativeMethods.BFFM_SELCHANGED: {
                    //IntPtr pathPtr = Marshal.AllocHGlobal((int)(260 * Marshal.SystemDefaultCharSize));

                    StringBuilder sb = new StringBuilder(260);
                    if (NativeMethods.SHGetPathFromIDList(lp, sb) != 0) {
                        NativeMethods.SendMessage(new HandleRef(null, hwnd), NativeMethods.BFFM_SETSTATUSTEXTW, 0, sb.ToString());
                        
                    }

                    //Marshal.FreeHGlobal(pathPtr);
                    break;
                }
            }

            return 0;
        }

        public string SelectFolder(string caption, string initialPath, IntPtr parentHandle) {
            _initialPath = initialPath;
            StringBuilder sb = new StringBuilder(256);
            IntPtr pidl = IntPtr.Zero;
            BROWSEINFO bi = new BROWSEINFO();
            bi.hwndOwner = parentHandle;
            bi.pidlRoot = IntPtr.Zero;
            bi.pszDisplayName = initialPath;
            bi.lpszTitle = caption;
            bi.ulFlags = NativeMethods.BIF_NEWDIALOGSTYLE | NativeMethods.BIF_SHAREABLE;
            bi.lpfn = new BrowseCallBackProc(OnBrowseEvent);
            bi.lParam = IntPtr.Zero;
            bi.iImage = 0;

            try {
                pidl = NativeMethods.SHBrowseForFolder(ref bi);
                if (0 == NativeMethods.SHGetPathFromIDList(pidl, sb)) {
                    return null;
                }
            }
            finally {
                // Caller is responsible for freeing this memory.
                Marshal.FreeCoTaskMem(pidl);
            }

            return sb.ToString();
        }
    }
}