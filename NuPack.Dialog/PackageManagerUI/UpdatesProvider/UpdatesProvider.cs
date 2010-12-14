using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class UpdatesProvider : PackagesProviderBase {

        private IVsPackageManager _packageManager;

        public UpdatesProvider(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources)
            : base(projectManager, resources) {

            _packageManager = packageManager;
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

        protected override void FillRootNodes() {
            var allNode = new UpdatesTreeNode(
                this,
                Resources.Dialog_RootNodeAll,
                RootNode,
                ProjectManager.LocalRepository,
                _packageManager.SourceRepository);

            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item) {
            IPackage package = item.PackageIdentity;
            if (package == null) {
                return false;
            }
            // only enable command on a Package in the Update provider if it not updated yet.

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.
            return ProjectManager.LocalRepository.GetPackages().Any(
                p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
        }

        protected override bool ExecuteCore(PackageItem item, ILicenseWindowOpener licenseWindowOpener) {
            // display license window if necessary
            DependencyResolver helper = new DependencyResolver(_packageManager.SourceRepository);
            IEnumerable<IPackage> licensePackages = helper.GetDependencies(item.PackageIdentity)
                                                          .Where(p => p.RequireLicenseAcceptance && !_packageManager.LocalRepository.Exists(p));
            if (licensePackages.Any()) {
                bool accepted = licenseWindowOpener.ShowLicenseWindow(licensePackages);
                if (!accepted) {
                    return false;
                }
            }

            _packageManager.UpdatePackage(ProjectManager, item.Id, new Version(item.Version), updateDependencies: true);
            return true;
        }

        protected override void OnExecuteCompleted(PackageItem item) {
            item.UpdateEnabledStatus();
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
                CommandName = Resources.Dialog_UpdateButton
            };
        }

        public override string NoItemsMessage {
            get {
                return Resources.Dialog_UpdatesProviderNoItem;
            }
        }
    }
}
