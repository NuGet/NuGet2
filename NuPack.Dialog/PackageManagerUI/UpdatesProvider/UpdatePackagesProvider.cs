using System.Linq;
using System.Windows;
using NuPack.VisualStudio;

namespace NuPack.Dialog.Providers {
    internal class UpdatePackagesProvider : OnlinePackagesProvider {

        private ResourceDictionary _resources;
        private object _mediumIconDataTemplate;

        public UpdatePackagesProvider(VSPackageManager packageManager, EnvDTE.Project activeProject, ResourceDictionary resources)
            : base(packageManager, activeProject, resources) {
            _resources = resources;
        }

        public override string Name {
            get {
                return Resources.Dialog_UpdateProvider;
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
                    _mediumIconDataTemplate = _resources["UpdatePackageItemTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override IQueryable<IPackage> GetQuery() {
            return ProjectManager.LocalRepository.GetUpdates(PackageManager.SourceRepository).AsQueryable();
        }
    }
}