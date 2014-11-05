using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public partial class PackageSolutionDetailControl : UserControl, IDetailControl
    {
        public PackageManagerControl Control { get; set; }

        private Solution Solution
        {
            get
            {
                var solution = Control.Target as Solution;
                Debug.Assert(solution != null, "Expected that the target would be a solution!");
                return solution;
            }
        }

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

        public async Task<IEnumerable<PackageAction>> ResolveActionsAsync()
        {
            var model = (PackageSolutionDetailControlModel)DataContext;
            var repo = Control.CreateActiveRepository();
            if (repo == null)
            {
                throw new InvalidOperationException(Resx.Resources.Error_NoActiveRepository);
            }
            var resolver = new ActionResolver(
                repo,
                new ResolutionContext()
                {
                    DependencyBehavior = model.SelectedDependencyBehavior.Behavior,
                    AllowPrerelease = Control.IncludePrerelease
                });

            var action = model.SelectedAction == Resx.Resources.Action_Uninstall ?
                PackageActionType.Uninstall :
                PackageActionType.Install;

            var targetProjects = model.Projects
                .Where(p => p.Selected)
                .Select(p => p.Project);
            return await resolver.ResolveActionsAsync(
                new PackageIdentity(model.Package.Id, model.SelectedVersion.Version),
                action,
                targetProjects,
                Solution);
        }

        public void Refresh()
        {
            var model = (PackageSolutionDetailControlModel)DataContext;
            if (model != null)
            {
                model.Refresh();
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
                var model = (PackageSolutionDetailControlModel)DataContext;
                return model.SelectedFileConflictAction.Action;
            }
        }    
    }
}