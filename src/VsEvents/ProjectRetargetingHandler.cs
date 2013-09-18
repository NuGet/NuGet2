using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.VsEvents
{
    public sealed class ProjectRetargetingHandler : IVsTrackProjectRetargetingEvents, IVsTrackBatchRetargetingEvents, IDisposable
    {
        private uint _cookieProjectRetargeting;
        private uint _cookieBatchRetargeting;
        private DTE _dte;
        private IVsTrackProjectRetargeting _vsTrackProjectRetargeting;
        private ErrorListProvider _errorListProvider;
        private IVsMonitorSelection _vsMonitorSelection;
        private string _platformRetargetingProject;

        private const string NETCore45 = ".NETCore,Version=v4.5";
        private const string Windows80 = "Windows, Version=8.0";
        private const string NETCore451 = ".NETCore,Version=v4.5.1";
        private const string Windows81 = "Windows, Version=8.1";

        /// <summary>
        /// Constructs and Registers ("Advises") for Project retargeting events if the IVsTrackProjectRetargeting service is available
        /// Otherwise, it simply exits
        /// </summary>
        /// <param name="dte"></param>
        public ProjectRetargetingHandler(DTE dte, IServiceProvider serviceProvider)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            IVsTrackProjectRetargeting vsTrackProjectRetargeting = serviceProvider.GetService(typeof(SVsTrackProjectRetargeting)) as IVsTrackProjectRetargeting;
            if (vsTrackProjectRetargeting != null)
            {
                _vsMonitorSelection = (IVsMonitorSelection)serviceProvider.GetService(typeof(IVsMonitorSelection));
                Debug.Assert(_vsMonitorSelection != null);
                _errorListProvider = new ErrorListProvider(serviceProvider);
                _dte = dte;
                _vsTrackProjectRetargeting = vsTrackProjectRetargeting;

                // Register for ProjectRetargetingEvents
                if (_vsTrackProjectRetargeting.AdviseTrackProjectRetargetingEvents(this, out _cookieProjectRetargeting) == VSConstants.S_OK)
                {
                    Debug.Assert(_cookieProjectRetargeting != 0);
                    _dte.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                    _dte.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
                }
                else
                {
                    _cookieProjectRetargeting = 0;
                }

                // Register for BatchRetargetingEvents. Using BatchRetargetingEvents, we need to detect platform retargeting
                if (_vsTrackProjectRetargeting.AdviseTrackBatchRetargetingEvents(this, out _cookieBatchRetargeting) == VSConstants.S_OK)
                {
                    Debug.Assert(_cookieBatchRetargeting != 0);
                    if (_cookieProjectRetargeting == 0)
                    {
                        // Register for dte Events only if they are not already registered for
                        _dte.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                        _dte.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
                    }
                }
                else
                {
                    _cookieBatchRetargeting = 0;
                }
            }
        }

        private void SolutionEvents_AfterClosing()
        {
            _errorListProvider.Tasks.Clear();
        }

        private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            // Clear the error list upon the first build action
            // Note that the retargeting error message is shown on the errorlistprovider this class creates
            // Hence, explicit clearing of the error list is required
            _errorListProvider.Tasks.Clear();

            if (Action != vsBuildAction.vsBuildActionClean)
            {
                ShowWarningsForPackageReinstallation(_dte.Solution);
            }
        }

        private void ShowWarningsForPackageReinstallation(Solution solution)
        {
            Debug.Assert(solution != null);

            foreach (Project project in solution.Projects)
            {
                IList<PackageReference> packageReferencesToBeReinstalled = ProjectRetargetingUtility.GetPackageReferencesMarkedForReinstallation(project);
                if (packageReferencesToBeReinstalled.Count > 0)
                {
                    Debug.Assert(project.IsNuGetInUse());
                    IVsHierarchy projectHierarchy = project.ToVsHierarchy();
                    ShowRetargetingErrorTask(packageReferencesToBeReinstalled.Select(p => p.Id), projectHierarchy, TaskErrorCategory.Warning, TaskPriority.Normal);
                }
            }
        }

        private void ShowRetargetingErrorTask(IEnumerable<string> packagesToBeReinstalled, IVsHierarchy projectHierarchy, TaskErrorCategory errorCategory, TaskPriority priority)
        {
            Debug.Assert(packagesToBeReinstalled != null && !packagesToBeReinstalled.IsEmpty());

            var errorText = String.Format(CultureInfo.CurrentCulture, Resources.ProjectUpgradeAndRetargetErrorMessage,
                    String.Join(", ", packagesToBeReinstalled));
            VsUtility.ShowError(_errorListProvider, errorCategory, priority, errorText, projectHierarchy);
        }

        #region IVsTrackProjectRetargetingEvents
        int IVsTrackProjectRetargetingEvents.OnRetargetingAfterChange(string projRef, IVsHierarchy pAfterChangeHier, string fromTargetFramework, string toTargetFramework)
        {
            _errorListProvider.Tasks.Clear();
            Project retargetedProject = VsUtility.GetProjectFromHierarchy(pAfterChangeHier);
            if (retargetedProject != null)
            {
                IList<IPackage> packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(retargetedProject);
                if (!packagesToBeReinstalled.IsEmpty())
                {
                    ShowRetargetingErrorTask(packagesToBeReinstalled.Select(p => p.Id), pAfterChangeHier, TaskErrorCategory.Error, TaskPriority.High);
                }
                ProjectRetargetingUtility.MarkPackagesForReinstallation(retargetedProject, packagesToBeReinstalled);
            }
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingBeforeChange(string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework, out bool pCanceled, out string ppReasonMsg)
        {
            pCanceled = false;
            ppReasonMsg = null;
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingBeforeProjectSave(string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingCanceledChange(string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingFailure(string projRef, IVsHierarchy pHier, string fromTargetFramework, string toTargetFramework)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsTrackBatchRetargetingEvents
        int IVsTrackBatchRetargetingEvents.OnBatchRetargetingBegin()
        {
            if (VsVersionHelper.IsVisualStudio2013)
            {
                Project project = _vsMonitorSelection.GetActiveProject();
                if (project != null)
                {
                    _platformRetargetingProject = null;
                    string frameworkName = project.GetTargetFramework();
                    if (NETCore45.Equals(frameworkName, StringComparison.OrdinalIgnoreCase) || Windows80.Equals(frameworkName, StringComparison.OrdinalIgnoreCase))
                    {
                        _platformRetargetingProject = project.UniqueName;
                    }
                }
            }
            return VSConstants.S_OK;
        }

        int IVsTrackBatchRetargetingEvents.OnBatchRetargetingEnd()
        {
            _errorListProvider.Tasks.Clear();
            if (_platformRetargetingProject != null)
            {
                try
                {
                    Project project = _dte.Solution.Item(_platformRetargetingProject);
                    if (project != null)
                    {
                        string frameworkName = project.GetTargetFramework();
                        if (NETCore451.Equals(frameworkName, StringComparison.OrdinalIgnoreCase) || Windows81.Equals(frameworkName, StringComparison.OrdinalIgnoreCase))
                        {
                            IList<IPackage> packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(project);
                            if (packagesToBeReinstalled.Count > 0)
                            {
                                // By asserting that NuGet is in use, we are also asserting that NuGet.VisualStudio.dll is already loaded
                                // Hence, it is okay to call project.ToVsHierarchy()
                                Debug.Assert(project.IsNuGetInUse());
                                IVsHierarchy projectHierarchy = project.ToVsHierarchy();
                                ShowRetargetingErrorTask(packagesToBeReinstalled.Select(p => p.Id), projectHierarchy, TaskErrorCategory.Error, TaskPriority.High);
                            }
                            ProjectRetargetingUtility.MarkPackagesForReinstallation(project, packagesToBeReinstalled);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // If the solution does not contain a project named '_platformRetargetingProject', it will throw ArgumentException
                }
                _platformRetargetingProject = null;
            }
            return VSConstants.S_OK;
        }
        #endregion

        public void Dispose()
        {
            // Nothing is initialized if _vsTrackProjectRetargeting is null. Check if it is not null
            if(_vsTrackProjectRetargeting != null)
            {
                _errorListProvider.Dispose();
                if(_cookieProjectRetargeting != 0)
                {
                    _vsTrackProjectRetargeting.UnadviseTrackProjectRetargetingEvents(_cookieProjectRetargeting);
                }

                if(_cookieBatchRetargeting != 0)
                {
                    _vsTrackProjectRetargeting.UnadviseTrackBatchRetargetingEvents(_cookieBatchRetargeting);
                }

                if (_cookieProjectRetargeting != 0 || _cookieBatchRetargeting != 0)
                {
                    _dte.Events.BuildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;
                    _dte.Events.SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
                }
            }
        }
    }
}
