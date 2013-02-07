using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    internal class UpdatesProvider : OnlineProvider
    {
        private readonly Project _project;
        private readonly IUpdateAllUIService _updateAllUIService;

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
                solutionManager)
        {
            _project = project;
            _updateAllUIService = providerServices.UpdateAllUIService;
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_UpdateProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 3.0f;
            }
        }

        public override bool RefreshOnNodeSelection
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsExecuteAllCommand
        {
            get
            {
                return true;
            }
        }

        public override string OperationName
        {
            get
            {
                return RepositoryOperationNames.Update;
            }
        }

        protected override PackagesTreeNodeBase CreateTreeNodeForPackageSource(PackageSource source, IPackageRepository sourceRepository)
        {
            return new UpdatesTreeNode(this, source.Name, RootNode, LocalRepository, sourceRepository);
        }

        public override bool CanExecute(PackageItem item)
        {
            IPackage package = item.PackageIdentity;
            if (package == null)
            {
                return false;
            }
            // only enable command on a Package in the Update provider if it not updated yet.

            // the specified package can be updated if the local repository contains a package 
            // with matching id and smaller version number.

            // Optimization due to bug #2008: if the LocalRepository is backed by a packages.config file, 
            // check the packages information directly from the file, instead of going through
            // the IPackageRepository interface, which could potentially connect to TFS.
            var packageLookup = LocalRepository as ILatestPackageLookup;
            if (packageLookup != null)
            {
                SemanticVersion localPackageVersion; 
                return packageLookup.TryFindLatestPackageById(item.Id, out localPackageVersion) &&
                       localPackageVersion < package.Version;
            }
            
            return LocalRepository.GetPackages().Any(
                p => p.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.Version < package.Version);
        }

        protected override void ExecuteCommand(IProjectManager projectManager, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations)
        {
            activePackageManager.UpdatePackage(
                projectManager, 
                item.PackageIdentity, 
                operations, 
                updateDependencies: true, 
                allowPrereleaseVersions: IncludePrerelease, 
                logger: this);
        }

        protected override bool ExecuteAllCore()
        {
            if (SelectedNode == null || SelectedNode.Extensions == null || SelectedNode.Extensions.Count == 0)
            {
                return false;
            }

            ShowProgressWindow();

            IVsPackageManager activePackageManager = GetActivePackageManager();
            Debug.Assert(activePackageManager != null);

            IDisposable action = activePackageManager.SourceRepository.StartOperation(OperationName);
            IProjectManager projectManager = activePackageManager.GetProjectManager(_project);

            try
            {
                bool accepted = ShowLicenseAgreementForAllPackages(activePackageManager);
                if (!accepted)
                {
                    return false;
                }

                RegisterPackageOperationEvents(activePackageManager, projectManager);
                activePackageManager.UpdatePackages(projectManager, updateDependencies: true, allowPrereleaseVersions: IncludePrerelease, logger: this);
                return true;
            }
            finally
            {
                UnregisterPackageOperationEvents(activePackageManager, projectManager);
                action.Dispose();
            }
        }

        protected bool ShowLicenseAgreementForAllPackages(IVsPackageManager activePackageManager)
        {
            var allOperations = new List<PackageOperation>();

            var allPackages = SelectedNode.GetPackages(String.Empty, IncludePrerelease).ToList();
            foreach (var package in allPackages)
            {
                var installWalker = new InstallWalker(
                    LocalRepository,
                    activePackageManager.SourceRepository,
                    _project.GetTargetFrameworkName(),
                    logger: this,
                    ignoreDependencies: false,
                    allowPrereleaseVersions: IncludePrerelease);

                var operations = installWalker.ResolveOperations(package);
                allOperations.AddRange(operations);
            }

            return ShowLicenseAgreement(activePackageManager, allOperations.Reduce());
        }

        protected override void OnExecuteCompleted(PackageItem item)
        {
            base.OnExecuteCompleted(item);

            // When this was the Update All command execution, 
            // an individual Update command may have updated all remaining packages.
            // If there are no more updates left, we hide the Update All button. 
            // 
            // However, we only want to do so if there's only one page of result, because
            // we don't want to download all packages in all pages just to check for this condition.
            if (SelectedNode != null && SelectedNode.TotalNumberOfPackages > 1 && SelectedNode.TotalPages == 1)
            {
                if (SelectedNode.Extensions.OfType<PackageItem>().All(p => !p.IsEnabled))
                {
                    _updateAllUIService.Hide();
                }
            }
        }

        public override void OnPackageLoadCompleted(PackagesTreeNodeBase selectedNode)
        {
            base.OnPackageLoadCompleted(selectedNode);
            UpdateNumberOfPackages(selectedNode);
        }

        private void UpdateNumberOfPackages(PackagesTreeNodeBase selectedNode)
        {
            if (selectedNode != null && !selectedNode.IsSearchResultsNode && selectedNode.TotalNumberOfPackages > 1)
            {
                // After performing Update All, if user switches to another page, we don't want to show 
                // the Update All button again. Here we check to make sure there's at least one enabled package.
                if (selectedNode.Extensions.OfType<PackageItem>().Any(p => p.IsEnabled))
                {
                    _updateAllUIService.Show();
                }
                else
                {
                    _updateAllUIService.Hide();
                }
            }
            else
            {
                _updateAllUIService.Hide();
            }
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            var localPackage = LocalRepository.FindPackagesById(package.Id)
                                              .OrderByDescending(p => p.Version)
                                              .FirstOrDefault();

            return new PackageItem(this, package, localPackage != null ? localPackage.Version : null)
            {
                CommandName = Resources.Dialog_UpdateButton,
                TargetFramework = _project.GetTargetFrameworkName()
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_UpdatesProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Dialog.Resources.Dialog_UpdateProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package)
        {
            if (package == null)
            {
                return Resources.Dialog_UpdateAllProgress;
            }

            return Resources.Dialog_UpdateProgress + package.ToString();
        }
    }
}