using NuGet.PackagingCore;
using NuGet.ProjectManagement;
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
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    // The DataContext of this control is DetailControlModel, i.e. either 
    // PackageSolutionDetailControlModel or PackageDetailControlModel.
    public partial class DetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }

        public DetailControl()
        {
            InitializeComponent();
            this.DataContextChanged += PackageSolutionDetailControl_DataContextChanged;
        }

        private void PackageSolutionDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is DetailControlModel)
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

        public void ScrollToHome()
        {
            _root.ScrollToHome();
        }

        public UserAction GetUserAction()
        {
            var model = (DetailControlModel)DataContext;
            var action = model.SelectedAction == Resx.Resources.Action_Uninstall ?
                PackageActionType.Uninstall :
                PackageActionType.Install;

            return new UserAction(
                action,
                new PackageIdentity(model.Id, model.SelectedVersion.Version));
        }

        //public async Task<IEnumerable<PackageAction>> ResolveActionsAsync(IExecutionContext logger)
        //{
        //    var model = (DetailControlModel)DataContext;
        //    var action = model.SelectedAction == Resx.Resources.Action_Uninstall ?
        //        PackageActionType.Uninstall :
        //        PackageActionType.Install;

        //    // Create resolver
        //    var repo = Control.CreateActiveRepository();
        //    if (action == PackageActionType.Uninstall)
        //    {
        //        // for uninstall, use local repo
        //        repo = Control.Target.TryGetFeature<SourceRepository>();
        //    }
        //    if (repo == null)
        //    {
        //        throw new InvalidOperationException(Resx.Resources.Error_NoActiveRepository);
        //    }
        //    var resolver = new ActionResolver(
        //        repo,
        //        new ResolutionContext()
        //        {
        //            DependencyBehavior = model.Options.SelectedDependencyBehavior.Behavior,
        //            AllowPrerelease = Control.IncludePrerelease,
        //            ForceRemove = model.Options.ForceRemove,
        //            RemoveDependencies = model.Options.RemoveDependencies
        //        });
        //    resolver.Logger = logger;

        //    IEnumerable<NuGetProject> targetProjects;
        //    var solutionModel = DataContext as PackageSolutionDetailControlModel;
        //    if (solutionModel != null)
        //    {
        //        targetProjects = solutionModel.Projects
        //           .Where(p => p.Selected)
        //           .Select(p => p.Project);
        //    }
        //    else
        //    {
        //        var project = Control.Target as NuGetProject;
        //        targetProjects = new[] { project };
        //        Debug.Assert(project != null);
        //    }

        //    return await resolver.ResolveActionsAsync(
        //        new PackageIdentity(model.Id, model.SelectedVersion.Version),
        //        action,
        //        targetProjects,
        //        Control.Target.OwnerSolution);
        //}

        public void Refresh()
        {
            var model = (DetailControlModel)DataContext;
            if (model != null)
            {
                model.Refresh();
            }
        }

        private void ActionButtonClicked(object sender, RoutedEventArgs e)
        {
            Control.PerformAction(this);
        }

        public FileConflictAction FileConflictAction
        {
            get
            {
                var model = (DetailControlModel)DataContext;
                return model.Options.SelectedFileConflictAction.Action;
            }
        }    
    }
}