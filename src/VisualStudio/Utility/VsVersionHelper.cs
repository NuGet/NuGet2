using System;
using EnvDTE;

namespace NuGet.VisualStudio {
    public static class VsVersionHelper {
        private static bool? _isVS2010;

        public static bool IsVisualStudio2010 {
            get {
                if (_isVS2010 == null) {
                    DTE dte = ServiceLocator.GetInstance<DTE>();
                    string vsVersion = dte.Version;
                    _isVS2010 = vsVersion.StartsWith("10", StringComparison.InvariantCultureIgnoreCase);
                }

                return _isVS2010.Value;
            }
        }
    }
}
