using System;
using System.Runtime.InteropServices;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("65E435C1-6970-4BBF-8842-5DBCB0707711")]
    public interface IVsPackageInstallerEvents
    {
        event VsPackageEventHandler PackageInstalling;
        event VsPackageEventHandler PackageInstalled;
        event VsPackageEventHandler PackageUninstalling;
        event VsPackageEventHandler PackageUninstalled;
    }
}
