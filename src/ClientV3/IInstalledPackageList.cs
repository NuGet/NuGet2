using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Tools
{
    public interface IInstalledPackageList
    {
        IEnumerable<PackageName> GetInstalledPackages();
        SemanticVersion GetInstalledVersion(string packageId);
        bool IsInstalled(string packageId, SemanticVersion packageVersion);
    }
}
