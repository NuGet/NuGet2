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
    public sealed class ProjectRetargetingHandler : IVsTrackProjectRetargetingEvents, IDisposable
    {
        private uint _cookie;
        private DTE _dte;
        private IVsTrackProjectRetargeting _vsTrackProjectRetargeting;
        private ErrorListProvider _errorListProvider;

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
                _errorListProvider = new ErrorListProvider(serviceProvider);
                _dte = dte;
                _vsTrackProjectRetargeting = vsTrackProjectRetargeting;

                if (_vsTrackProjectRetargeting.AdviseTrackProjectRetargetingEvents(this, out _cookie) == VSConstants.S_OK)
                {
                    Debug.Assert(_cookie != 0);
                    _dte.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                    _dte.Events.SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
                }
                else
                {
                    _cookie = 0;
                }
            }
        }

        private void SolutionEvents_AfterClosing()
        {
            _errorListProvider.Tasks.Clear();
        }

        private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            // Clear the error list upon the first build action. This includes building, rebuilding, cleaning and so on
            // Note that the retargeting error message is shown on the errorlistprovider this class creates
            // Hence, explicit clearing of the error list is required
            _errorListProvider.Tasks.Clear();
        }

        private void ShowRetargetingError(IList<IPackage> packagesToBeReinstalled, IVsHierarchy pAfterChangeHier)
        {
            Debug.Assert(packagesToBeReinstalled != null && !packagesToBeReinstalled.IsEmpty());

            ErrorTask retargetErrorTask = new ErrorTask();
            retargetErrorTask.Text = String.Format(CultureInfo.CurrentCulture, Resources.ProjectUpgradeAndRetargetErrorMessage,
                String.Join(", ", packagesToBeReinstalled.Select(p => p.Id)));
            retargetErrorTask.ErrorCategory = TaskErrorCategory.Error;
            retargetErrorTask.Category = TaskCategory.BuildCompile;
            retargetErrorTask.HierarchyItem = pAfterChangeHier;
            _errorListProvider.Tasks.Add(retargetErrorTask);
            _errorListProvider.BringToFront();
            _errorListProvider.ForceShowErrors();
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
                    ShowRetargetingError(packagesToBeReinstalled, pAfterChangeHier);
                }
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

        public void Dispose()
        {
            _errorListProvider.Dispose();
            if (_cookie != 0 && _vsTrackProjectRetargeting != null)
            {
                _vsTrackProjectRetargeting.UnadviseTrackProjectRetargetingEvents(_cookie);
                _dte.Events.BuildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;
                _dte.Events.SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
            }
        }
    }
}
