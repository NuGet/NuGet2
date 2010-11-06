using System;

namespace NuGet.VisualStudio {
    public interface IPackageSourceSettingsManager {
        string ActivePackageSourceString { get; set; }
        bool IsFirstRunning { get; set; }
        string PackageSourcesString { get; set; }
    }
}
