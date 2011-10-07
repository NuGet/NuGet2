using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Windows.Threading;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI {
    [Export(typeof(IUserNotifierServices))]
    internal class UserNotifierServices : IUserNotifierServices {
        private readonly Dispatcher _uiDispatcher;

        public UserNotifierServices() {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        bool IUserNotifierServices.ShowLicenseWindow(IEnumerable<IPackage> packages) {
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

        public IEnumerable<Project> ShowProjectSelectorWindow(
            string instructionText,
            IPackage package,
            Predicate<Project> checkedStateSelector, 
            Predicate<Project> enabledStateSelector) {
            if (!_uiDispatcher.CheckAccess()) {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<string, IPackage, Predicate<Project>, Predicate<Project>, IEnumerable<Project>>(ShowProjectSelectorWindow),
                    instructionText,
                    package,
                    checkedStateSelector,
                    enabledStateSelector);

                return (IEnumerable<Project>)result;
            }

            var viewModel = new SolutionExplorerViewModel(
                ServiceLocator.GetInstance<DTE>().Solution,
                package,
                checkedStateSelector,
                enabledStateSelector);

            // only show the solution explorer window if there is at least one compatible project
            if (viewModel.HasProjects) {
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
            else {
                // if there is no project compatible with the selected package, show an error message and return
                MessageHelper.ShowWarningMessage(
                    String.Format(
                        CultureInfo.CurrentCulture, 
                        Resources.Dialog_NoCompatibleProject,
                        package.Id,
                        Environment.NewLine + String.Join(Environment.NewLine, package.GetSupportedFrameworks())),
                    title: null);
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

        public bool? ShowRemoveDependenciesWindow(string message) {
            if (!_uiDispatcher.CheckAccess()) {
                object result = _uiDispatcher.Invoke(
                    new Func<string, bool?>(ShowRemoveDependenciesWindow),
                    message);
                return (bool?)result;
            }

            return MessageHelper.ShowQueryMessage(message, title: null, showCancelButton: true);
        }
    }
}