using System.Collections.Generic;

namespace NuPack.Dialog.PackageManagerUI {
    internal interface ILicenseWindowOpener {
        bool ShowLicenseWindow(IEnumerable<IPackage> dataContext);
    }
}
