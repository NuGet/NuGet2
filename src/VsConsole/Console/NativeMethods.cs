using System;
using System.Runtime.InteropServices;

namespace NuGetConsole {
    internal static class NativeMethods {

        // Size of VARIANTs in 32 bit systems
        public const int VariantSize = 16;

        [DllImport("Oleaut32.dll", PreserveSig = false)]
        public static extern void VariantClear(IntPtr var);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="dwhkl"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwLayout">Use 0 for the current (calling) thread</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetKeyboardLayout(int dwLayout);

        /// <summary>
        /// If the specified input locale identifier is not already loaded, the function loads and activates the input locale identifier for the current thread.
        /// </summary>
        public const int KLF_ACTIVE = 0x00000001;

        /// <summary>
        /// Substitutes the specified input locale identifier with another locale preferred by the user. The system starts with this flag set, and it is recommended that your application always use this flag. The substitution occurs only if the registry key HKEY_CURRENT_USER\Keyboard\Layout\Substitutes explicitly defines a substitution locale. For example, if the key includes the value name "00000409" with value "00010409", loading the U.S. English layout ("00000409") causes the Dvorak U.S. English layout ("00010409") to be loaded instead. The system uses KLF_SUBSTITUTE_OK when booting, and it is recommended that all applications use this value when loading input locale identifiers to ensure that the user's preference is selected.
        /// </summary>
        public const int KLF_SUBSTITUTE_OK = 0x00000002;
    }
}
