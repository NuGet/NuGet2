using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NuGet.VsEvents
{
    public sealed class ProjectUpgradeHandler : IVsSolutionEvents, IVsSolutionEventsProjectUpgrade, IDisposable
    {
        private uint _cookie;
        private IVsSolution2 _vsSolution2;

        /// <summary>
        /// Constructs and Registers ("Advises") for Project retargeting events if the IVsSolutionEvents service is available
        /// Otherwise, it simply exits
        /// </summary>
        public ProjectUpgradeHandler(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            IVsSolution2 vsSolution2 = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
            if (vsSolution2 != null)
            {
                _vsSolution2 = vsSolution2;
                if (vsSolution2.AdviseSolutionEvents(this, out _cookie) == VSConstants.S_OK)
                {
                    Debug.Assert(_cookie != 0);
                }
                else
                {
                    _cookie = 0;
                }
            }
        }

        #region IVsSolutionEventsProjectUpgrade
        int IVsSolutionEventsProjectUpgrade.OnAfterUpgradeProject(IVsHierarchy pHierarchy, uint fUpgradeFlag, string bstrCopyLocation, SYSTEMTIME stUpgradeTime, IVsUpgradeLogger pLogger)
        {
            Debug.Assert(pHierarchy != null);

            Project upgradedProject = VsUtility.GetProjectFromHierarchy(pHierarchy);

            if (upgradedProject != null)
            {
                IList<IPackage> packagesToBeReinstalled = ProjectRetargetingUtility.GetPackagesToBeReinstalled(upgradedProject);

                if (!packagesToBeReinstalled.IsEmpty())
                {
                    pLogger.LogMessage((int)__VSUL_ERRORLEVEL.VSUL_ERROR, upgradedProject.Name, upgradedProject.Name,
                        String.Format(CultureInfo.CurrentCulture, Resources.ProjectUpgradeAndRetargetErrorMessage, String.Join(", ", packagesToBeReinstalled.Select(p => p.Id))));
                }
            }
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsSolutionEvents (mandatory but unused implementation)
        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }
        #endregion

        public void Dispose()
        {
            if (_cookie != 0 && _vsSolution2 != null)
            {
                _vsSolution2.UnadviseSolutionEvents(_cookie);
            }
        }
    }
}
