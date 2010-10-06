using System.Linq;
using System.Windows;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledPackagesProvider : OnlinePackagesProvider {
        private const string XamlTemplateKey = "InstalledPackagesTileTemplate";
        private readonly ResourceDictionary _resources;
        private object _mediumIconDataTemplate;

        public InstalledPackagesProvider(ResourceDictionary resources)
            : base(resources, false) {
            _resources = resources;
        }

        public override string Name {
            get {
                return "Installed";
            }
        }

        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources[XamlTemplateKey];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override IQueryable<IPackage> GetQuery() {
            return ProjectManager.LocalRepository.GetPackages();
        }
    }
}
