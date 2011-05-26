using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using EnvDTE;
using NuGet.Dialog.PackageManagerUI;
using NuGet.VisualStudio;

namespace NuGet.Dialog {
    [Export(typeof(IProjectSelectorService))]
    internal class ProjectSelectorService : IProjectSelectorService {

        private readonly Dispatcher _uiDispatcher;
        public ProjectSelectorService() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public IEnumerable<Project> ShowProjectSelectorWindow(Func<Project, bool> checkedStateSelector, Func<Project, bool> enabledStateSelector) {
            if (!_uiDispatcher.CheckAccess()) {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<Func<Project, bool>, Func<Project, bool>, IEnumerable<Project>>(ShowProjectSelectorWindow),
                    checkedStateSelector,
                    enabledStateSelector);

                return (IEnumerable<Project>)result;
            }

            var viewModel = new SolutionExplorerViewModel(
                ServiceLocator.GetInstance<DTE>().Solution,
                checkedStateSelector,
                enabledStateSelector);
            var window = new SolutionExplorer() {
                DataContext = viewModel
            };

            bool? dialogResult = window.ShowModal();
            if (dialogResult ?? false) {
                return viewModel.GetSelectedProjects();
            }
            else {
                return null;
            }
        }

        public void ShowSummaryWindow(object failedProjects) {
            if (!_uiDispatcher.CheckAccess()) {
                _uiDispatcher.Invoke(new Action<object>(ShowSummaryWindow), failedProjects);
                return;
            }

            var window = new SummaryWindow() {
                DataContext = failedProjects
            };

            window.ShowModal();
        }
    }
}