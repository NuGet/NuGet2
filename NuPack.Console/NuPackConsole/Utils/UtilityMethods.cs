using System;

namespace NuPackConsole
{
    class UtilityMethods
    {
        public static void ThrowIfArgumentNull<T>(T arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException();
            }
        }

        public static void ThrowIfArgumentNullOrEmpty(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentException();
            }
        }
    }
}
