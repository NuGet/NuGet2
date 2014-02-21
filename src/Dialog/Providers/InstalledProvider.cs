using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionsExplorer;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers
{
    /// <summary>
    /// IVsExtensionsProvider implementation responsible for gathering
    /// a list of installed packages which will be shown in the Add Package dialog.
    /// </summary>
    internal class InstalledProvider : PackagesProviderBase
    {
        private readonly IVsPackageManager _packageManager;
        private readonly Project _project;
        private readonly IUserNotifierServices _userNotifierServices;
        private readonly IPackageRestoreManager _packageRestoreManager;
        private readonly FrameworkName _targetFramework;

        public InstalledProvider(
            IVsPackageManager packageManager,
            Project project,
            IPackageRepository localRepository,
            ResourceDictionary resources,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager,
            IPackageRestoreManager packageRestoreManager)
            : base(localRepository, resources, providerServices, progressProvider, solutionManager)
        {
            if (packageManager == null)
            {
                throw new ArgumentNullException("packageManager");
            }

            _packageManager = packageManager;
            _project = project;
            _targetFramework = _project.GetTargetFrameworkName();
            _userNotifierServices = providerServices.UserNotifierServices;
            _packageRestoreManager = packageRestoreManager;
            _packageRestoreManager.PackagesMissingStatusChanged += OnMissPackagesChanged;
        }

        protected IVsPackageManager PackageManager
        {
            get
            {
                return _packageManager;
            }
        }

        public override string Name
        {
            get
            {
                return Resources.Dialog_InstalledProvider;
            }
        }

        public override float SortOrder
        {
            get
            {
                return 1.0f;
            }
        }

        public override bool ShowPrereleaseComboBox
        {
            get
            {
                // for Installed tab, we always show prerelease packages. hence, hide the combobox
                return false;
            }
        }

        internal override bool IncludePrerelease
        {
            get
            {
                // we always shows prerelease packages in the Installed tab
                return true;
            }
            set
            {
            }
        }

        public override bool RefreshOnNodeSelection
        {
            get
            {
                return true;
            }
        }

        public override IEnumerable<string> SupportedFrameworks
        {
            get
            {
                string targetFramework = _project.GetTargetFramework();
                return targetFramework != null ? new[] { targetFramework } : new string[0];
            }
        }

        protected override IList<IVsSortDescriptor> CreateSortDescriptors()
        {
            return new List<IVsSortDescriptor> 
                   {
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortAscending), new[] { "Title", "Id" }, ListSortDirection.Ascending),
                        new PackageSortDescriptor(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.Dialog_SortOption_Name, Resources.Dialog_SortDescending), new[] { "Title", "Id" }, ListSortDirection.Descending)
                   };
        }

        protected override void FillRootNodes()
        {
            var allNode = new SimpleTreeNode(
                this,
                Resources.Dialog_RootNodeAll,
                RootNode,
                LocalRepository,
                collapseVersion: false);
            RootNode.Nodes.Add(allNode);
        }

        public override bool CanExecute(PackageItem item)
        {
            return true;
        }

        protected override bool ExecuteCore(PackageItem item)
        {
            if (_project.SupportsINuGetProjectSystem())
            {
                ShowProgressWindow();
                UninstallPackageFromProject(_project, item, (bool)false);
                HideProgressWindow();
                return true;
            }
            else
            {
                CheckDependentPackages(item.PackageIdentity, LocalRepository, _targetFramework);

                bool? removeDependencies = AskRemoveDependency(
                    item.PackageIdentity,
                    new[] { LocalRepository },
                    new[] { _targetFramework });

                if (removeDependencies == null)
                {
                    // user presses Cancel
                    return false;
                }

                ShowProgressWindow();
                UninstallPackageFromProject(_project, item, (bool)removeDependencies);
                HideProgressWindow();
                return true;
            }
        }

        protected void CheckDependentPackages(
            IPackage package,
            IPackageRepository localRepository,
            FrameworkName targetFramework)
        {
            // check if there is any other package depends on this package.
            // if there is, throw to cancel the uninstallation
            var dependentsWalker = new DependentsWalker(localRepository, targetFramework);
            IList<IPackage> dependents = dependentsWalker.GetDependents(package).ToList();
            if (dependents.Count > 0)
            {
                ShowProgressWindow();
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PackageHasDependents,
                        package.GetFullName(),
                        String.Join(", ", dependents.Select(d => d.GetFullName()))
                    )
                );
            }
        }

        protected bool? AskRemoveDependency(
            IPackage package,
            IList<IPackageRepository> localRepositories,
            IList<FrameworkName> targetFrameworks)
        {
            Debug.Assert(localRepositories.Count == targetFrameworks.Count);

            var allOperations = new List<PackageOperation>();

            for (int i = 0; i < localRepositories.Count; i++)
            {
                var uninstallWalker = new UninstallWalker(
                    localRepositories[i],
                    new DependentsWalker(localRepositories[i], targetFrameworks[i]),
                    targetFrameworks[i],
                    logger: NullLogger.Instance,
                    removeDependencies: true,
                    forceRemove: false)
                    {
                        ThrowOnConflicts = false
                    };
                var operations = uninstallWalker.ResolveOperations(package);
                allOperations.AddRange(operations);
            }

            allOperations = allOperations.Reduce().ToList();

            var uninstallPackageNames = (from o in allOperations
                                         where o.Action == PackageAction.Uninstall && !PackageEqualityComparer.IdAndVersion.Equals(o.Package, package)
                                         select o.Package)
                                         .Distinct(PackageEqualityComparer.IdAndVersion)
                                         .Select(p => p.ToString())
                                         .ToList();

            bool? removeDependencies = false;
            if (uninstallPackageNames.Count > 0)
            {
                // show each dependency package on one line
                String packageNames = String.Join(Environment.NewLine, uninstallPackageNames);
                String message = String.Format(CultureInfo.CurrentCulture, Resources.Dialog_RemoveDependencyMessage, package)
                        + Environment.NewLine
                        + Environment.NewLine
                        + packageNames;

                removeDependencies = _userNotifierServices.ShowRemoveDependenciesWindow(message);
            }

            return removeDependencies;
        }

        protected void InstallPackageToProject(Project project, IPackage package, bool includePrerelease)
        {
            IProjectManager projectManager = null;
            try
            {
                projectManager = PackageManager.GetProjectManager(project);
                // make sure the package is not installed in this project before proceeding
                if (!projectManager.IsInstalled(package))
                {
                    RegisterPackageOperationEvents(PackageManager, projectManager);
                    PackageManager.InstallPackage(projectManager, package.Id, package.Version, ignoreDependencies: false, allowPrereleaseVersions: includePrerelease, logger: this);
                }
            }
            finally
            {
                if (projectManager != null)
                {
                    UnregisterPackageOperationEvents(PackageManager, projectManager);
                }
            }
        }

        protected void UninstallPackageFromProject(Project project, PackageItem item, bool removeDependencies)
        {
            IProjectManager projectManager = null;
            try
            {
                projectManager = PackageManager.GetProjectManager(project);
                // make sure the package is installed in this project before proceeding
                if (projectManager.IsInstalled(item.PackageIdentity))
                {
                    RegisterPackageOperationEvents(PackageManager, projectManager);
                    PackageManager.UninstallPackage(projectManager, item.Id, version: item.PackageIdentity.Version, forceRemove: false, removeDependencies: removeDependencies, logger: this);
                }
            }
            finally
            {
                if (projectManager != null)
                {
                    UnregisterPackageOperationEvents(PackageManager, projectManager);
                }
            }
        }

        protected override void OnExecuteCompleted(PackageItem item)
        {
            if (SelectedNode != null)
            {
                // after every uninstall operation, just refresh the current node because
                // when packages are uninstalled, the number of pages may decrease.
                SelectedNode.Refresh(resetQueryBeforeRefresh: true);
            }
            else
            {
                base.OnExecuteCompleted(item);
            }
        }

        public override IVsExtension CreateExtension(IPackage package)
        {
            return new PackageItem(this, package)
            {
                CommandName = Resources.Dialog_UninstallButton,
                TargetFramework = _project.GetTargetFrameworkName()
            };
        }

        public override string NoItemsMessage
        {
            get
            {
                return Resources.Dialog_InstalledProviderNoItem;
            }
        }

        public override string ProgressWindowTitle
        {
            get
            {
                return Dialog.Resources.Dialog_UninstallProgress;
            }
        }

        protected override string GetProgressMessage(IPackage package)
        {
            return Resources.Dialog_UninstallProgress + package.ToString();
        }

        private void OnMissPackagesChanged(object sender, PackagesMissingStatusEventArgs e)
        {
            // after packages are restored, refresh the installed tab to show those packages.
            if (!e.PackagesMissing)
            {
                if (SelectedNode != null)
                {
                    SelectedNode.Refresh(resetQueryBeforeRefresh: true);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            // to avoid memory leak, we need to unsubscribe from the event
            _packageRestoreManager.PackagesMissingStatusChanged -= OnMissPackagesChanged;
        }
    }
}