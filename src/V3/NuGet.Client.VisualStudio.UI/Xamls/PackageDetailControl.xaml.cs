using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using NuGet.Client.Resolution;
using System.Diagnostics;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

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

        private void PackageDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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
                Control.PreviewActions(actions);
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
                return await resolver.ResolveActionsAsync(
                    packageDetail.Package.Id, 
                    packageDetail.Package.Version, 
                    action,
                    Control.Target.TargetProjects);
            }
            finally
            {
                Control.SetBusy(false);
            }
        }

        private async void PerformPackageAction(PackageActionType action)
        {
            var model = (PackageDetailControlModel)DataContext;
            Control.SetBusy(true);
            var progressDialog = new ProgressDialog(model.SelectedFileConflictAction.Action);
            try
            {
                var actions = await ResolveActions(action);

                // show license agreeement
                bool acceptLicense = Control.ShowLicenseAgreement(actions);
                if (!acceptLicense)
                {
                    return;
                }

                // This should only be called in cases where there is a single target
                Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");
                Debug.Assert(Control.Target is ProjectInstallationTarget, "PackageDetailControl should only be used when there is only one target project!");

                // Create the execution context
                var context = new ExecutionContext((ProjectInstallationTarget)Control.Target);

                // Create the executor and execute the actions                
                progressDialog.Owner = Window.GetWindow(Control);
                progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progressDialog.Show();
                var executor = new ActionExecutor();
                await executor.ExecuteActionsAsync(actions, context, progressDialog);

                Control.UpdatePackageStatus();
                UpdatePackageStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                progressDialog.RequestToClose();
                Control.SetBusy(false);
            }
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
                _actionButton.Content = Resx.Resources.Button_Uninstall;
            }
            else
            {
                _actionButton.Content = Resx.Resources.Button_Install;
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
            if (installedPackage != null)
            {
                var installedVersion = installedPackage.Identity.Version;
                model.CreateVersions(installedVersion);
            }
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

        private void ActionButtonClicked(object sender, RoutedEventArgs e)
        {
            if ((string)_actionButton.Content == Resx.Resources.Button_Install)
            {
                PerformPackageAction(PackageActionType.Install);
            }
            else
            {
                PerformPackageAction(PackageActionType.Uninstall);
            }
        }

        private void PreviewButtonClicked(object sender, RoutedEventArgs e)
        {
            if ((string)_actionButton.Content == Resx.Resources.Button_Install)
            {
                Preview(PackageActionType.Install);
            }
            else
            {
                Preview(PackageActionType.Uninstall);
            }

        }
    }
}