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
using Resx = NuGet.Client.VisualStudio.UI.Resources;
using NuGet.Client.Resolution;
using System.Diagnostics;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageDetail.xaml
    /// </summary>
    public partial class PackageDetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }

        public PackageDetailControl()
        {
            InitializeComponent();
            this.DataContextChanged += PackageDetailControl_DataContextChanged;
        }

        void PackageDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is PackageDetailControlModel)
            {
                _root.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {   
                _root.Visibility = System.Windows.Visibility.Collapsed;
            }
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
            try
            {
                var actions = await ResolveActions(action);

                PreviewActions(actions);
            }
            catch (InvalidOperationException ex)
            {
                // TODO: Is this the only reason for this exception???
                MessageBox.Show("Temporary Message! Clean this up!" + Environment.NewLine + ex.Message, "Temporary Message");
            }
        }

        private async Task<IEnumerable<PackageAction>> ResolveActions(PackageActionType action)
        {
            var packageDetail = (PackageDetailControlModel)DataContext;
            
            Control.SetBusy(true);
            try
            {
                // Create a resolver
                var resolver = new ActionResolver(
                    Control.Sources.ActiveRepository,
                    Control.Target,
                    new ResolutionContext()
                    {
                        DependencyBehavior = packageDetail.SelectedDependencyBehavior.Behavior,
                        AllowPrerelease = false
                    });

                // Resolve actions
                return await resolver.ResolveActionsAsync(packageDetail.Package.Id, packageDetail.Package.Version, action);
            }
            finally
            {
                Control.SetBusy(false);
            }
        }

        private void PreviewActions(
            IEnumerable<PackageAction> actions)
        {
            MessageBox.Show("TODO: Better UI." + Environment.NewLine + String.Join(Environment.NewLine, actions.Select(a => a.ToString())));
            // Show result
            // values:
            // 1: unchanged
            // 0: deleted
            // 2: added
            //var packageStatus = Control.Target
            //    .Installed
            //    .GetInstalledPackageReferences()
            //    .Select(p => p.Identity)
            //    .ToDictionary(p => /* key */ p, _ => /* value */ 1);

            //foreach (var action in actions)
            //{
            //    if (action.ActionType == PackageActionType.Install)
            //    {
            //        packageStatus[action.PackageName] = 2;
            //    }
            //    else if (action.ActionType == PackageActionType.Uninstall)
            //    {
            //        packageStatus[action.PackageName] = 0;
            //    }
            //}

            //var w = new PreviewWindow(
            //    unchanged: packageStatus.Where(v => v.Value == 1).Select(v => v.Key),
            //    deleted: packageStatus.Where(v => v.Value == 0).Select(v => v.Key),
            //    added: packageStatus.Where(v => v.Value == 2).Select(v => v.Key));
            //w.Owner = Window.GetWindow(Control);
            //w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //w.ShowDialog();
        }

        private async void PerformPackageAction(PackageActionType action)
        {
            var actions = await ResolveActions(action);

            // show license agreeement
            bool acceptLicense = ShowLicenseAgreement(actions);
            if (!acceptLicense)
            {
                return;
            }

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");
            Debug.Assert(Control.Target is ProjectInstallationTarget, "PackageDetailControl should only be used when there is only one target project!");
            
            // Create the executor and execute the actions
            Control.SetBusy(true);
            try
            {
                var executor = new ProjectActionExecutor((ProjectInstallationTarget)Control.Target);
                await executor.ExecuteActionsAsync(actions);
            }
            finally
            {
                Control.SetBusy(false);
            }

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

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");

            var isInstalled = Control.Target.TargetProjects.Single().InstalledPackages.IsInstalled(model.Package.Id, model.Package.Version);
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

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");

            UpdateInstallUninstallButton();
            var installedPackage = Control.Target.TargetProjects.Single().InstalledPackages.GetInstalledPackage(model.Package.Id);
            var installedVersion = installedPackage.Identity.Version;
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