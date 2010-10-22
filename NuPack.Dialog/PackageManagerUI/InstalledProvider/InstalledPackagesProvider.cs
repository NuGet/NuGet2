using System.Linq;
using System.Windows;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledPackagesProvider : OnlinePackagesProvider {
        private const string XamlTemplateKey = "InstalledPackageItemTemplate";
        private readonly ResourceDictionary _resources;
        private object _mediumIconDataTemplate;

        public InstalledPackagesProvider(VsPackageManager packageManager, EnvDTE.Project activeProject, ResourceDictionary resources)
            : base(packageManager, activeProject, resources) {
            _resources = resources;
        }

        public override string Name {
            get {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                return true;
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
