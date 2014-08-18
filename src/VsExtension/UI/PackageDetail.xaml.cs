using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NuGet.Dialog.PackageManagerUI;
using NuGet.Resolver;
using NuGet.VisualStudio;

namespace NuGet.Tools
{
    /// <summary>
    /// Interaction logic for PackageDetail.xaml
    /// </summary>
    public partial class PackageDetail : UserControl
    {
        public PackageManagerControl Control { get; set; }

        private enum Metadatas
        {
            Authors,
            Owners,
            License,
            Donwloads,
            DatePublished,
            ProjectInformation,
            Tags
        }

        // item in the dependency installation behavior list view
        private class DependencyBehavior
        {
            public string Text
            {
                get;
                private set;
            }

            public DependencyVersion? DependencyVersion
            {
                get;
                private set;
            }

            public DependencyBehavior(string text, DependencyVersion? dependencyVersion)
            {
                Text = text;
                DependencyVersion = dependencyVersion;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private static DependencyBehavior[] _dependencyBehaviors = new DependencyBehavior[] {
            new DependencyBehavior("Ignore Dependencies", null),
            new DependencyBehavior("Lowest", DependencyVersion.Lowest),
            new DependencyBehavior("HighestPath", DependencyVersion.HighestPatch),
            new DependencyBehavior("HighestMinor", DependencyVersion.HighestMinor),
            new DependencyBehavior("Highest", DependencyVersion.Highest),
        };

        public PackageDetail()
        {
            InitializeComponent();
            this.DataContextChanged += PackageDetail_DataContextChanged;

            this.Visibility = System.Windows.Visibility.Collapsed;

            _dependencyBehavior.Items.Clear();
            foreach (var d in _dependencyBehaviors)
            {
                _dependencyBehavior.Items.Add(d);
            }
            _dependencyBehavior.SelectedItem = _dependencyBehaviors[1];

            foreach (var v in Enum.GetValues(typeof(FileConflictResolution)))
            {
                _fileConflictAction.Items.Add(v);
            }
            _fileConflictAction.SelectedItem = FileConflictResolution.Overwrite;
        }

        private void PackageDetail_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Visibility = DataContext is PackageDetailControlModel ?
                System.Windows.Visibility.Visible :
                System.Windows.Visibility.Collapsed;
        }

        private void Versions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            var v = (VersionForDisplay)_versions.SelectedItem;            
            model.SelectVersion(v == null ? null : v.Version);
            UpdateInstallUninstallButton();
        }

        private async void Preview(PackageAction action)
        {
            var projectManager = Control.Model.PackageManager.GetProjectManager(Control.Model.Project);
            Control.SetBusy(true);
            var actions = await ResolveActionsAsync(action, projectManager);
            Control.SetBusy(false);

            PreviewActions(actions, projectManager);
        }

        private async Task<IEnumerable<Resolver.PackageAction>> ResolveActionsAsync(
            PackageAction action,
            IProjectManager projectManager)
        {
            var d = (DependencyBehavior)_dependencyBehavior.SelectedItem;
            var resolver = new ActionResolver()
            {
                IgnoreDependencies = !d.DependencyVersion.HasValue,
                AllowPrereleaseVersions = false
            };

            if (d.DependencyVersion.HasValue)
            {
                resolver.DependencyVersion = d.DependencyVersion.Value;
            }

            var package = ((PackageDetailControlModel)DataContext).Package.Package;

            var actions = await Task.Factory.StartNew(
                () =>
                {
                    resolver.AddOperation(action, package, projectManager);
                    return resolver.ResolveActions();
                });
            return actions;
        }

        private void PreviewActions(
            IEnumerable<Resolver.PackageAction> actions,
            IProjectManager projectManager)
        {
            // Show result
            // values:
            // 1: unchanged
            // 0: deleted
            // 2: added
            var packageStatus = new Dictionary<IPackage, int>(PackageEqualityComparer.IdAndVersion);
            foreach (var p in projectManager.LocalRepository.GetPackages())
            {
                packageStatus[p] = 1;
            }

            foreach (var action in actions)
            {
                var projectAction = action as PackageProjectAction;
                if (projectAction == null)
                {
                    continue;
                }

                if (projectAction.ActionType == PackageActionType.Install)
                {
                    packageStatus[projectAction.Package] = 2;
                }
                else if (projectAction.ActionType == PackageActionType.Uninstall)
                {
                    packageStatus[projectAction.Package] = 0;
                }
            }

            var w = new PreviewWindow(
                unchanged: packageStatus.Where(v => v.Value == 1).Select(v => v.Key),
                deleted: packageStatus.Where(v => v.Value == 0).Select(v => v.Key),
                added: packageStatus.Where(v => v.Value == 2).Select(v => v.Key));
            w.Owner = Window.GetWindow(Control);
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog();
        }

        private async void PerformPackageAction(PackageAction action)
        {
            var projectManager = Control.Model.PackageManager.GetProjectManager(Control.Model.Project);
            Control.SetBusy(true);
            var actions = await ResolveActionsAsync(action, projectManager);
            Control.SetBusy(false);

            // show license agreeement
            bool acceptLicense = ShowLicenseAgreement(actions);
            if (!acceptLicense)
            {
                return;
            }

            ActionExecutor executor = new ActionExecutor();
            executor.Execute(actions);

            Control.UpdatePackageStatus();
            UpdatePackageStatus();
        }

        private void UpdateInstallUninstallButton()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            bool isInstalled = Control.Model.LocalRepo.Exists(model.Package.Id, model.Package.Version);
            if (isInstalled)
            {
                _dropdownButton.SetItems(
                    new[] { "Uninstall", "Uninstall Preview" });
            }
            else
            {
                _dropdownButton.SetItems(
                    new[] { "Install", "Install Preview" });
            }
        }

        private void UpdatePackageStatus()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            UpdateInstallUninstallButton();
            var installedPackage = Control.Model.LocalRepo.FindPackage(model.Package.Id);
            var installedVersion = installedPackage != null ? installedPackage.Version : null;
            model.CreateVersions(installedVersion);
        }

        protected bool ShowLicenseAgreement(IEnumerable<Resolver.PackageAction> operations)
        {
            var licensePackages = operations.Where(
                    op => op.ActionType == PackageActionType.AddToPackagesFolder &&
                        op.Package.RequireLicenseAcceptance)
                    .Select(op => op.Package);

            // display license window if necessary
            if (licensePackages.Any())
            {
                IUserNotifierServices uss = new UserNotifierServices();
                bool accepted = uss.ShowLicenseWindow(
                    licensePackages.Distinct<IPackage>(PackageEqualityComparer.IdAndVersion));
                if (!accepted)
                {
                    return false;
                }
            }

            return true;
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void _dropdownButton_Clicked(object sender, DropdownButtonClickEventArgs e)
        {
            switch (e.ButtonText)
            {
                case "Install":
                    PerformPackageAction(PackageAction.Install);
                    break;

                case "Install Preview":
                    Preview(PackageAction.Install);
                    break;

                case "Uninstall":
                    PerformPackageAction(PackageAction.Uninstall);
                    break;

                case "Uninstall Preview":
                    Preview(PackageAction.Uninstall);
                    break;
            }
        }
    }
}