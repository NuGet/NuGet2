using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using NuGet.Client.Resolution;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageDetail.xaml
    /// </summary>
    public partial class PackageDetailControl : UserControl, IDetailControl
    {
        public PackageManagerControl Control { get; set; }

        private Project Project
        {
            get
            {
                var solution = Control.Target as Project;
                Debug.Assert(solution != null, "Expected that the target would be a project!");
                return solution;
            }
        }

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

        public async Task<IEnumerable<PackageAction>> ResolveActionsAsync()
        {
            PackageActionType action =
                ((string)_actionButton.Content == Resx.Resources.Action_Install ||
                (string)_actionButton.Content == Resx.Resources.Action_Update) ?
                PackageActionType.Install :
                PackageActionType.Uninstall;
            var packageDetail = (PackageDetailControlModel)DataContext;

            // Create a resolver
            var repo = Control.CreateActiveRepository();
            if (action == PackageActionType.Uninstall)
            {
                repo = Project.TryGetFeature<SourceRepository>();
            }

            if (repo == null)
            {
                throw new InvalidOperationException(Resx.Resources.Error_NoActiveRepository);
            }

            var resolver = new ActionResolver(
                repo,
                new ResolutionContext()
                {
                    DependencyBehavior = packageDetail.SelectedDependencyBehavior.Behavior,
                    AllowPrerelease = Control.IncludePrerelease
                });

            // Resolve actions
            return await resolver.ResolveActionsAsync(
                new PackageIdentity(packageDetail.Package.Id, packageDetail.Package.Version),
                action,
                new[] { Project },
                Project.OwnerSolution);
        }

        private void UpdateInstallUninstallButton()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            var isInstalled = Project.InstalledPackages.IsInstalled(model.Package.Id, model.Package.Version);
            if (isInstalled)
            {
                _actionButton.Content = Resx.Resources.Action_Uninstall;
                _options.Visibility = Visibility.Collapsed;
            }
            else
            {
                var isOtherVersionInstalled = Project.InstalledPackages.IsInstalled(model.Package.Id);
                if (isOtherVersionInstalled)
                {
                    _actionButton.Content = Resx.Resources.Action_Update;
                }
                else
                {
                    _actionButton.Content = Resx.Resources.Action_Install;
                }
                _options.Visibility = Visibility.Visible;
            }
        }

        // Refresh the control after package install/uninstall.
        public void Refresh()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            UpdateInstallUninstallButton();
            var installedPackage = Project.InstalledPackages.GetInstalledPackage(model.Package.Id);
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
            Control.PerformAction(this);
        }

        private void PreviewButtonClicked(object sender, RoutedEventArgs e)
        {
            Control.Preview(this);
        }

        public FileConflictAction FileConflictAction
        {
            get
            {
                var model = (PackageDetailControlModel)DataContext;
                return model.SelectedFileConflictAction.Action;
            }
        }
    }
}