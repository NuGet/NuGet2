using System;
using System.Runtime.InteropServices;

namespace NuGetConsole {
    internal static class NativeMethods {

        // Size of VARIANTs in 32 bit systems
        public const int VariantSize = 16;

        [DllImport("Oleaut32.dll", PreserveSig = false)]
        public static extern void VariantClear(IntPtr var);
    }
}
