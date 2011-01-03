using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;
using NuGetConsole.Host.PowerShellProvider;

namespace NuGet.Dialog.Providers {
    internal class UpdatesProvider : PackagesProviderBase {

        private IVsPackageManager _packageManager;
        private Lazy<InstallWalker> _walker;
        private ILicenseWindowOpener _licenseWindowOpener;

        public UpdatesProvider(
            IVsPackageManager packageManager,
            Project project,
            IProjectManager projectManager, 
            ResourceDictionary resources,
            ILicenseWindowOpener licenseWindowOpener,
            IProgressWindowOpener progressWindowOpener,
            IScriptExecutor scriptExecutor)
            : base(project, projectManager, resources, progressWindowOpener, scriptExecutor) {

            _packageManager = packageManager;
            _licenseWindowOpener = licenseWindowOpener;

            _walker = new Lazy<InstallWalker>(
                () => new InstallWalker(ProjectManager.LocalRepository, _packageManager.SourceRepository, NullLogger.Instance, ignoreDependencies: false));
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

        protected override bool ExecuteCore(PackageItem item) {

            IList<PackageOperation> operations = _walker.Value.ResolveOperations(item.PackageIdentity).ToList();

            IEnumerable<IPackage> scriptPackages = from o in operations
                                                   where o.Package.HasPowerShellScript()
                                                   select o.Package;
            
            if (scriptPackages.Any() && !RegistryHelper.CheckIfPowerShell2Installed()) {
                throw new InvalidOperationException(Resources.Dialog_PackageHasPSScript);
            }

            IList<IPackage> licensePackages = (from o in operations
                                               where o.Action == PackageAction.Install && o.Package.RequireLicenseAcceptance && !_packageManager.LocalRepository.Exists(o.Package)
                                               select o.Package).ToList();

            // display license window if necessary
            if (licensePackages.Count > 0) {
                // hide the progress window if we are going to show license window
                HideProgressWindow();

                bool accepted = _licenseWindowOpener.ShowLicenseWindow(licensePackages);
                if (!accepted) {
                    return false;
                }

                ShowProgressWindow();
            }

            try {
                RegisterPackageOperationEvents(_packageManager);
                _packageManager.UpdatePackage(ProjectManager, item.PackageIdentity, operations, updateDependencies: true, logger: this);
            }
            finally {
                UnregisterPackageOperationEvents(_packageManager);
            }
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

        public override string ProgressWindowTitle {
            get {
                return Dialog.Resources.Dialog_UpdateProgress;
            }
        }
    }
}