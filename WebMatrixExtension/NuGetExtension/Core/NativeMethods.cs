using System;
using System.Runtime.InteropServices;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Class that provides P/Invoke access to Windows APIs.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// P/Invokes to the Windows DeleteObject API.
        /// </summary>
        /// <param name="hObject">HANDLE to object to delete.</param>
        /// <returns>True if successful.</returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
