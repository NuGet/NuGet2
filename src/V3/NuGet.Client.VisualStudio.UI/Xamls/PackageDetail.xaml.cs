using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using NuGet.Client;
using NuGet.Client.Resolution;

namespace NuGet.Client.VisualStudio.UI
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
        private class DependencyBehaviorItem
        {
            public string Text
            {
                get;
                private set;
            }

            public DependencyBehavior Behavior
            {
                get;
                private set;
            }

            public DependencyBehaviorItem(string text, DependencyBehavior dependencyBehavior)
            {
                Text = text;
                Behavior = dependencyBehavior;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private static DependencyBehaviorItem[] _dependencyBehaviors = new[] {
            new DependencyBehaviorItem("Ignore Dependencies", DependencyBehavior.Ignore),
            new DependencyBehaviorItem("Lowest", DependencyBehavior.Lowest),
            new DependencyBehaviorItem("HighestPath", DependencyBehavior.HighestPatch),
            new DependencyBehaviorItem("HighestMinor", DependencyBehavior.HighestMinor),
            new DependencyBehaviorItem("Highest", DependencyBehavior.Highest),
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

            foreach (var v in Enum.GetValues(typeof(FileConflictAction)))
            {
                _fileConflictAction.Items.Add(v);
            }
            _fileConflictAction.SelectedItem = FileConflictAction.Overwrite;
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

        private async void Preview(PackageActionType action)
        {
            var actions = await ResolveActions(action);

            PreviewActions(actions);
        }

        private async Task<IEnumerable<PackageAction>> ResolveActions(PackageActionType action)
        {
            var package = (PackageDetailControlModel)DataContext;
            Control.SetBusy(true);

            // Create a resolver
            var resolver = new ActionResolver(
                Control.Sources.ActiveRepository,
                Control.Target,
                new ResolutionContext()
                {
                    DependencyBehavior = ((DependencyBehaviorItem)_dependencyBehavior.SelectedItem).Behavior,
                    AllowPrerelease = false
                });
            
            // Resolve actions
            var actions = await resolver.ResolveActionsAsync(package.Package.Id, package.Package.Version, action);

            Control.SetBusy(false);
            return actions;
        }

        private void PreviewActions(
            IEnumerable<PackageAction> actions)
        {
            // Show result
            // values:
            // 1: unchanged
            // 0: deleted
            // 2: added
            var packageStatus = Control.Target
                .GetInstalledPackages()
                .ToDictionary(p => /* key */ p, _ => /* value */ 1);

            foreach (var action in actions)
            {
                if (action.ActionType == PackageActionType.Install)
                {
                    packageStatus[action.PackageName] = 2;
                }
                else if (action.ActionType == PackageActionType.Uninstall)
                {
                    packageStatus[action.PackageName] = 0;
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

        private void PerformPackageAction(PackageActionType action)
        {
            //var actions = await ResolveActions(action);

            //// show license agreeement
            //bool acceptLicense = ShowLicenseAgreement(actions);
            //if (!acceptLicense)
            //{
            //    return;
            //}

            //await Session.CreateActionExecutor().ExecuteActions(actions);

            //Control.UpdatePackageStatus();
            //UpdatePackageStatus();
        }

        private void UpdateInstallUninstallButton()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            var isInstalled = Control.Target.IsInstalled(model.Package.Id, model.Package.Version);
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
            var installedVersion = Control.Target.GetInstalledVersion(model.Package.Id);
            model.CreateVersions(installedVersion);
        }

        protected bool ShowLicenseAgreement(IEnumerable<PackageAction> operations)
        {
            var licensePackages = operations.Where(op => 
                op.ActionType == PackageActionType.Install &&
                op.Package.Value<bool>("requireLicenseAcceptance"));

            // display license window if necessary
            if (licensePackages.Any())
            {
                // Hacky distinct without writing a custom comparer
                var licenseModels = licensePackages
                    .GroupBy(a => Tuple.Create(a.Package["id"], a.Package["version"]))
                    .Select(g =>
                    {
                        dynamic p = g.First().Package;
                        var authors = String.Join(", ",
                            ((JArray)(p.authors)).Cast<JValue>()
                            .Select(author => author.Value as string));

                        return new PackageLicenseInfo(
                            p.id.Value,
                            p.licenseUrl.Value,
                            authors);
                    });

                bool accepted = Control.UI.PromptForLicenseAcceptance(licenseModels);
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
                Control.UI.LaunchExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void _dropdownButton_Clicked(object sender, DropdownButtonClickEventArgs e)
        {
            switch (e.ButtonText)
            {
                case "Install":
                    PerformPackageAction(PackageActionType.Install);
                    break;

                case "Install Preview":
                    Preview(PackageActionType.Install);
                    break;

                case "Uninstall":
                    PerformPackageAction(PackageActionType.Uninstall);
                    break;

                case "Uninstall Preview":
                    Preview(PackageActionType.Uninstall);
                    break;
            }
        }
    }
}