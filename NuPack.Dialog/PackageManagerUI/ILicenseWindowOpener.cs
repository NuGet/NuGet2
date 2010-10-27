using System.Collections.Generic;

namespace NuGet.Dialog.PackageManagerUI {
    internal interface ILicenseWindowOpener {
        bool ShowLicenseWindow(IEnumerable<IPackage> dataContext);
    }
}
