using System;

namespace NuPackConsole {
    internal static class UtilityMethods {
        public static void ThrowIfArgumentNull<T>(T arg) {
            if (arg == null) {
                throw new ArgumentNullException("arg");
            }
        }

        public static void ThrowIfArgumentNullOrEmpty(string arg) {
            if (string.IsNullOrEmpty(arg)) {
                throw new ArgumentException("Invalid argument", "arg");
            }
        }
    }
}
