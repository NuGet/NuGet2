using System.Runtime.InteropServices;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("AF63941E-6BA8-4FEC-9827-8E4D1113F231")]
    public interface IVsPackageUninstaller
    {
        void UninstallPackage(Project project, string packageId, bool removeDependencies);
    }
}
