using System;

namespace NuGet.VisualStudio {
    public interface IPackageSourceSettingsManager {
        string ActivePackageSourceString { get; set; }
        string PackageSourcesString { get; set; }
    }
}
