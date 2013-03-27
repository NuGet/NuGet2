using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Threading;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.PackageManagerUI
{
    internal class UserNotifierServices : IUserNotifierServices
    {
        private readonly Dispatcher _uiDispatcher;

        public UserNotifierServices()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        bool IUserNotifierServices.ShowLicenseWindow(IEnumerable<IPackage> packages)
        {
            if (_uiDispatcher.CheckAccess())
            {
                return ShowLicenseWindow(packages);
            }
            else
            {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(new Func<object, bool>(ShowLicenseWindow), packages);
                return (bool)result;
            }
        }

        private bool ShowLicenseWindow(object dataContext)
        {
            var licenseWidow = new LicenseAcceptanceWindow()
            {
                DataContext = dataContext
            };

            // call ShowModal() instead of ShowDialog() so that the dialog
            // automatically centers within parent window
            using (NuGetEventTrigger.Instance.TriggerEventBeginEnd(
                NuGetEvent.LicenseWindowBegin,
                NuGetEvent.LicenseWindowEnd))
            {
                bool? dialogResult = licenseWidow.ShowModal();
                return dialogResult ?? false;
            }
        }

        public IEnumerable<Project> ShowProjectSelectorWindow(
            string instructionText,
            IPackage package,
            Predicate<Project> checkedStateSelector,
            Predicate<Project> enabledStateSelector)
        {
            if (!_uiDispatcher.CheckAccess())
            {
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
            if (viewModel.HasProjects)
            {
                var window = new SolutionExplorer()
                {
                    DataContext = viewModel
                };
                window.InstructionText.Text = instructionText;

                bool? dialogResult = null;
                using (NuGetEventTrigger.Instance.TriggerEventBeginEnd(
                    NuGetEvent.SelectProjectDialogBegin,
                    NuGetEvent.SelectProjectDialogEnd))
                {
                    dialogResult = window.ShowModal();
                }

                if (dialogResult ?? false)
                {
                    return viewModel.GetSelectedProjects();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                IEnumerable<FrameworkName> supportedFrameworks = package.GetSupportedFrameworks()
                                                                        .Where(name => name != null && name != VersionUtility.UnsupportedFrameworkName);
                string errorMessage = supportedFrameworks.Any() ?
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Dialog_NoCompatibleProject,
                        package.Id,
                        Environment.NewLine + String.Join(Environment.NewLine, supportedFrameworks)) :
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Dialog_NoCompatibleProjectNoFrameworkNames,
                        package.Id);

                // if there is no project compatible with the selected package, show an error message and return
                MessageHelper.ShowWarningMessage(errorMessage, title: null);
                return null;
            }
        }

        public void ShowSummaryWindow(object failedProjects)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                _uiDispatcher.Invoke(new Action<object>(ShowSummaryWindow), failedProjects);
                return;
            }

            var window = new SummaryWindow()
            {
                DataContext = failedProjects
            };

            window.ShowModal();
        }

        public bool? ShowRemoveDependenciesWindow(string message)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Func<string, bool?>(ShowRemoveDependenciesWindow),
                    message);
                return (bool?)result;
            }

            return MessageHelper.ShowQueryMessage(message, title: null, showCancelButton: true);
        }

        public FileConflictResolution ShowFileConflictResolution(string question)
        {
            if (!_uiDispatcher.CheckAccess())
            {
                object result = _uiDispatcher.Invoke(
                    new Func<string, FileConflictResolution>(ShowFileConflictResolution),
                    question);
                return (FileConflictResolution)result;
            }

            var window = new FileConflictDialog
            {
                Question = question
            };

            if (window.ShowModal() ?? false)
            {
                return window.UserSelection;
            }

            return FileConflictResolution.IgnoreAll;
        }
    }
}