using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NuGet.Client.Resolution;
using Resx = NuGet.Client.VisualStudio.UI.Resources;
using System.Linq;

namespace NuGet.Client.VisualStudio.UI
{
    public partial class PackageSolutionDetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }

        public PackageSolutionDetailControl()
        {
            InitializeComponent();
            this.DataContextChanged += PackageSolutionDetailControl_DataContextChanged;
        }

        private void PackageSolutionDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is PackageSolutionDetailControlModel)
            {
                _root.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _root.Visibility = System.Windows.Visibility.Collapsed;
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

        private async void ActionButtonClicked(object sender, RoutedEventArgs e)
        {
            var model = (PackageSolutionDetailControlModel)DataContext;

            IEnumerable<PackageAction> actions = null;
            Control.SetBusy(true);
            try
            {
                var resolver = new ActionResolver(
                    Control.Sources.ActiveRepository,
                    Control.Target,
                    new ResolutionContext()
                    {
                        DependencyBehavior = model.SelectedDependencyBehavior.Behavior,
                        AllowPrerelease = false
                    });

                var action = model.SelectedAction == Resx.Resources.Action_Uninstall ?
                    PackageActionType.Uninstall :
                    PackageActionType.Install;

                var targetProjects = model.Projects
                    .Where(p => p.Selected)
                    .Select(p => p.Project);

                actions = await resolver.ResolveActionsAsync(
                    model.Package.Id,
                    model.Package.Version,
                    action,
                    targetProjects);
            }
            finally
            {
                Control.SetBusy(false);
            }

            MessageBox.Show("TODO: Better UI." + Environment.NewLine + String.Join(Environment.NewLine, actions.Select(a => a.ToString())));
        }
    }
}
