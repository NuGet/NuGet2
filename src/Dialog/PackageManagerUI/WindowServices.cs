using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI {
    [Export(typeof(IWindowServices))]
    internal class WindowServices : IWindowServices {
        private readonly Dispatcher _uiDispatcher;

        public WindowServices() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        bool IWindowServices.ShowLicenseWindow(IEnumerable<IPackage> packages) {
            if (_uiDispatcher.CheckAccess()) {
                return ShowLicenseWindow(packages);
            }
            else {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(new Func<object, bool>(ShowLicenseWindow), packages);
                return (bool)result;
            }
        }

        private bool ShowLicenseWindow(object dataContext) {
            var licenseWidow = new LicenseAcceptanceWindow() {
                DataContext = dataContext
            };

            // call ShowModal() instead of ShowDialog() so that the dialog
            // automatically centers within parent window
            bool? dialogResult = licenseWidow.ShowModal();
            return dialogResult ?? false;
        }

        public IEnumerable<Project> ShowProjectSelectorWindow(string instructionText, Func<Project, bool> checkedStateSelector, Func<Project, bool> enabledStateSelector) {
            if (!_uiDispatcher.CheckAccess()) {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<string, Func<Project, bool>, Func<Project, bool>, IEnumerable<Project>>(ShowProjectSelectorWindow),
                    instructionText,
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
            window.InstructionText.Text = instructionText;

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

        public bool AskToRemoveDependencyPackages(string message) {
            if (!_uiDispatcher.CheckAccess()) {
                object result = _uiDispatcher.Invoke(
                    new Func<string, bool>(AskToRemoveDependencyPackages), 
                    message);
                return (bool)result;
            }

            return MessageHelper.ShowQueryMessage(message, null);
        }
    }
}