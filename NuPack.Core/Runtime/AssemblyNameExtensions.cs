using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NuGet.Runtime {
    public static class AssemblyNameExtensions {
        public static string GetPublicKeyTokenString(this AssemblyName assemblyName) {
            return String.Join(String.Empty, assemblyName.GetPublicKeyToken()
                                                         .Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
