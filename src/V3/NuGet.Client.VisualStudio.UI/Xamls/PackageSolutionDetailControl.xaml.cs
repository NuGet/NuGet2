using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NuGet.Client.Resolution;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

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

            Control.SetBusy(true);
            var progressDialog = new ProgressDialog();
            try
            {
                IEnumerable<PackageAction> actions = null;
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
                MessageBox.Show("TODO: Better UI." + Environment.NewLine + String.Join(Environment.NewLine, actions.Select(a => a.ToString())));

                var executor = new ActionExecutor();
                progressDialog.Owner = Window.GetWindow(Control);
                progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progressDialog.Show();

                var context = new ExecutionContext((CoreInteropInstallationTargetBase)Control.Target);
                await executor.ExecuteActionsAsync(actions, context, progressDialog);
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
    }
}