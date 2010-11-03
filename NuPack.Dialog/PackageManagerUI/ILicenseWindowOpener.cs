using System.Collections.Generic;

namespace NuGet.Dialog.PackageManagerUI {
    public interface ILicenseWindowOpener {
        bool ShowLicenseWindow(IEnumerable<IPackage> packages);
    }
}
