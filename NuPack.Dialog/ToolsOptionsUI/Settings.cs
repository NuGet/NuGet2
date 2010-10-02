using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuPack.VisualStudio;

namespace NuPack.Dialog.ToolsOptionsUI {
    internal static class Settings {
        private static VSPackageSourceProvider _packageSourceProvider;

        public static VSPackageSourceProvider PackageSourceProvider {
            get {
                if (_packageSourceProvider == null) {
                    _packageSourceProvider = VSPackageSourceProvider.GetSourceProvider(DTEExtensions.DTE);
                }

                return _packageSourceProvider;
            }
        }

        public static string RepositoryServiceUri {
            get {
                PackageSource activePackageSource = PackageSourceProvider.ActivePackageSource;
                return (activePackageSource != null) ? activePackageSource.Source : string.Empty;
            }
        }
    }
}