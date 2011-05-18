using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.VisualStudio;
using NuGetConsole.Host.PowerShellProvider;

namespace NuGet.Dialog.Providers {
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of packages from a package feed which will be shown in the Add NuGet dialog.
    /// </summary>
    internal class OnlineProvider : PackagesProviderBase {
        private readonly IPackageRepositoryFactory _packageRepositoryFactory;
        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly ProviderServices _providerServices;
        private readonly Project _project;

        public OnlineProvider(
            Project project,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) :
            base(localRepository, resources, providerServices, progressProvider, solutionManager) {

            _packageRepositoryFactory = packageRepositoryFactory;
            _packageSourceProvider = packageSourceProvider;
            _packageManagerFactory = packageManagerFactory;
            _providerServices = providerServices;
            _project = project;
        }

        public override string Name {
            get {
                return Resources.Dialog_OnlineProvider;
            }
        }

        public override float SortOrder {
            get {
                return 2.0f;
            }
        }

        public override bool RefreshOnNodeSelection {
            get {
                // only refresh if the current node doesn't have any extensions
                return (SelectedNode == null || SelectedNode.Extensions.Count == 0);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We want to suppress all errors to show an empty node.")]
        protected override void FillRootNodes() {
            var packageSources = _packageSourceProvider.GetPackageSourcesWithAggregate();

            // create one tree node per package source
            foreach (var source in packageSources) {
                PackagesTreeNodeBase node = null;
                try {
                    var repository = new LazyRepository(_packageRepositoryFactory, source);
                    node = CreateTreeNodeForPackageSource(source, repository);
                }
                catch (Exception exception) {
                    // exception occurs if the Source value is invalid. In which case, adds an empty tree node in place.
                    node = new EmptyTreeNode(this, source.Name, RootNode);
                    ExceptionHelper.WriteToActivityLog(exception);
                }

                RootNode.Nodes.Add(node);
            }
        }

        protected virtual PackagesTreeNodeBase CreateTreeNodeForPackageSource(PackageSource source, IPackageRepository repository) {
            return new SimpleTreeNode(this, source.Name, RootNode, repository);
        }

        protected internal virtual IVsPackageManager GetActivePackageManager() {
            if (SelectedNode == null) {
                return null;
            }
            else if (SelectedNode.IsSearchResultsNode) {
                PackagesSearchNode searchNode = (PackagesSearchNode)SelectedNode;
                SimpleTreeNode baseNode = (SimpleTreeNode)searchNode.BaseNode;
                return _packageManagerFactory.CreatePackageManager(baseNode.Repository);
            }
            else {
                var selectedNode = SelectedNode as SimpleTreeNode;
                return (selectedNode != null) ? _packageManagerFactory.CreatePackageManager(selectedNode.Repository) : null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't want one failed project to affect the other projects.")]
        protected override bool ExecuteCore(PackageItem item) {
            var activePackageManager = GetActivePackageManager();
            Debug.Assert(activePackageManager != null);

            var walker = new InstallWalker(
                LocalRepository,
                activePackageManager.SourceRepository,
                this,
                ignoreDependencies: false);

            IList<PackageOperation> operations = walker.ResolveOperations(item.PackageIdentity).ToList();

            IList<IPackage> scriptPackages = (from o in operations
                                              where o.Package.HasPowerShellScript()
                                              select o.Package).ToList();

            if (scriptPackages.Count > 0) {
                if (!RegistryHelper.CheckIfPowerShell2Installed()) {
                    throw new InvalidOperationException(Resources.Dialog_PackageHasPSScript);
                }
            }

            IEnumerable<IPackage> licensePackages = from o in operations
                                                    where o.Action == PackageAction.Install && o.Package.RequireLicenseAcceptance && !activePackageManager.LocalRepository.Exists(o.Package)
                                                    select o.Package;

            // display license window if necessary
            if (licensePackages.Any()) {
                // hide the progress window if we are going to show license window
                HideProgressWindow();

                bool accepted = _providerServices.LicenseWindow.ShowLicenseWindow(licensePackages);
                if (!accepted) {
                    return false;
                }

                ShowProgressWindow();
            }

            return ExecuteAfterLicenseAggrement(item, activePackageManager, operations);
        }

        protected virtual bool ExecuteAfterLicenseAggrement(PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            ExecuteCommandOnProject(_project, item, activePackageManager, operations);
            return true;
        }

        protected void ExecuteCommandOnProject(Project activeProject, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            IProjectManager projectManager = null;
            try {
                projectManager = activePackageManager.GetProjectManager(activeProject);
                RegisterPackageOperationEvents(activePackageManager, projectManager);
                ExecuteCommand(projectManager, item, activePackageManager, operations);
            }
            finally {
                if (projectManager != null) {
                    UnregisterPackageOperationEvents(activePackageManager, projectManager);
                }
            }
        }

        protected virtual void ExecuteCommand(IProjectManager projectManager, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            activePackageManager.InstallPackage(projectManager, item.PackageIdentity, operations, ignoreDependencies: false, logger: this);
        }

        public override bool CanExecute(PackageItem item) {
            // Only enable command on a Package in the Online provider if it is not installed yet
            return !LocalRepository.Exists(item.PackageIdentity);
        }

        public override IVsExtension CreateExtension(IPackage package) {
            return new PackageItem(this, package, null) {
                CommandName = Resources.Dialog_InstallButton
            };
        }

        public override string NoItemsMessage {
            get {
                return Resources.Dialog_OnlineProviderNoItem;
            }
        }

        public override string ProgressWindowTitle {
            get {
                return Dialog.Resources.Dialog_InstallProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package) {
            return Resources.Dialog_InstallProgress + package.ToString();
        }
    }
}