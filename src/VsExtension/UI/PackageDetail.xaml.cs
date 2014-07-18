using NuGet.Dialog.PackageManagerUI;
using NuGet.Resolver;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        void PackageDetail_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Visibility = DataContext is PackageDetailControlModel  ? 
                System.Windows.Visibility.Visible :
                System.Windows.Visibility.Collapsed;
        }        

        /*
        private void UpdatePackageDetail()
        {
            _dependencies.Items.Clear();
            if (_package == null)
            {
                _id.Text = "";
                _description.Text = "";

                _dropdownButton.IsEnabled = false;                
            }
            else
            {
                _id.Text = _package.Id;
                _description.Text = _package.Description;

                

                // metadata
                SetMetadata(Metadatas.Authors, String.Join(", ", _package.Authors));
                SetMetadata(Metadatas.Owners, String.Join(", ", _package.Owners));
                SetMetadata(Metadatas.License, _package.LicenseUrl, "View License");
                SetMetadata(Metadatas.Donwloads, _package.DownloadCount.ToString());
                SetMetadata(Metadatas.DatePublished, 
                    _package.Published.HasValue ? _package.Published.Value.ToString("d"): "");
                SetMetadata(Metadatas.ProjectInformation, _package.ProjectUrl, "Project Information");
                SetMetadata(Metadatas.Tags, _package.Tags);

                // dependencies
                foreach (var dependencySet in _package.DependencySets)
                {
                    if (dependencySet.TargetFramework != null)
                    {
                        _dependencies.Items.Add(new TextBlock()
                        {
                            Text = dependencySet.TargetFramework.ToString(),
                            FontWeight = FontWeights.DemiBold,
                            Margin = new Thickness(10, 0, 0, 0)
                        });
                    }

                    foreach (var d in dependencySet.Dependencies)
                    {
                        _dependencies.Items.Add(new TextBlock()
                        {
                            Text = d.ToString(),
                            TextWrapping = System.Windows.TextWrapping.Wrap,
                            Margin = new Thickness(20, 0, 0, 0)
                        });
                    }
                }
            }
        }

        private void SetMetadata(Metadatas metadatas, Uri uri, string text)
        {
            var textBlock = _metadataControls[metadatas];
            textBlock.Inlines.Clear();

            if (uri != null)
            {
                var hyperLink = new Hyperlink(new Run(text))
                {
                    NavigateUri = uri
                };
                hyperLink.Click += hyperLink_Click;

                textBlock.Inlines.Add(hyperLink);
                textBlock.ToolTip = uri.ToString();
            }
        }

        void hyperLink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperLink = sender as Hyperlink;
            if (hyperLink == null)
            {
                return;
            }

            UriHelper.OpenExternalLink(hyperLink.NavigateUri);
        }

        private void SetMetadata(Metadatas metadatas, string text)
        {
            _metadataControls[metadatas].Text = text;
        }


        } */

        private void Versions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            model.SelectVersion((SemanticVersion)_versions.SelectedItem);
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

            _dropdownButton.IsEnabled = true;

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
