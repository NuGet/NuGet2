using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class UpdatesProvider : OnlineProvider {

        public UpdatesProvider(
            Project project,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) : 
            base(
                project,
                localRepository, 
                resources, 
                packageRepositoryFactory, 
                packageSourceProvider, 
                packageManagerFactory, 
                providerServices, 
                progressProvider,
                solutionManager) {
        }

        public override string Name {
            get {
                return Resources.Dialog_UpdateProvider;
            }
        }

        public override float SortOrder {
            get {
                return 3.0f;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                return true;
            }
        }

        protected override PackagesTreeNodeBase CreateTreeNodeForPackageSource(PackageSource source, IPackageRepository sourceRepository) {
            return new UpdatesTreeNode(this, source.Name, RootNode, LocalRepository, sourceRepository);
        }

        public override bool CanExecute(PackageItem item) {
            IPackage package = item.PackageIdentity;
            if (package == null) {
                return false;
            }
            // only enable command on a Package in the Update provider if it not updated yet.

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.
            return LocalRepository.GetPackages().Any(
                p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
        }

        protected override void ExecuteCommand(IProjectManager projectManager, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            activePackageManager.UpdatePackage(projectManager, item.PackageIdentity, operations, updateDependencies: true, logger: this);
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

        public override string ProgressWindowTitle {
            get {
                return Dialog.Resources.Dialog_UpdateProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package) {
            return Resources.Dialog_UpdateProgress + package.ToString();
        }
    }
}