using System.Linq;
using System.Windows;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of extensions from the extension respository
    /// which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledPackagesProvider : OnlinePackagesProvider {
        public InstalledPackagesProvider(ResourceDictionary resources)
            : base(resources, false) {
        }

        public override string Name {
            get {
                return "Installed";
            }
        }

        public override IQueryable<IPackage> GetQuery() {           
            return ProjectManager.LocalRepository.GetPackages();
        }
    }
}
