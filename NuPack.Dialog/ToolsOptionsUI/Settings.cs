using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using NuPack.VisualStudio;

namespace NuPack.Dialog.ToolsOptionsUI {
    internal static class Settings {
        private static VSPackageSourceProvider _packageSourceProvider;

        public static VSPackageSourceProvider PackageSourceProvider {
            get {
                if (_packageSourceProvider == null) {
                    var serviceProvider = Utilities.ServiceProvider;
                    DTE dte = (DTE)serviceProvider.GetService(typeof(SDTE));
                    _packageSourceProvider = VSPackageSourceProvider.Create(dte);
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