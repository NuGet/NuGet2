using System;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio {
    [ComImport]
    [Guid("65E435C1-6970-4BBF-8842-5DBCB0707711")]
    public interface IVsPackageInstallerEvents {
        event Action<IVsPackageMetadata> PackageInstalling;
        event Action<IVsPackageMetadata> PackageInstalled;
        event Action<IVsPackageMetadata> PackageUninstalling;
        event Action<IVsPackageMetadata> PackageUninstalled;
    }
}
