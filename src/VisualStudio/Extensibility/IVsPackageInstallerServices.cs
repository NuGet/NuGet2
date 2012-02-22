using System.Collections.Generic;
using System.Runtime.InteropServices;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [ComImport]
    [Guid("B858E847-4920-4313-9D3B-176BB0D2F5C2")]
    public interface IVsPackageInstallerServices
    {
        IEnumerable<IVsPackageMetadata> GetInstalledPackages();
        bool IsPackageInstalled(Project project, string id);
        bool IsPackageInstalled(Project project, string id, SemanticVersion version);
    }
}
