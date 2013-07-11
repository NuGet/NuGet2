using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    public static class DTEExtensions
    {
        public static Project GetActiveProject(this IVsMonitorSelection vsMonitorSelection)
        {
            return VsUtility.GetActiveProject(vsMonitorSelection);
        }

        public static bool GetIsSolutionNodeSelected(this IVsMonitorSelection vsMonitorSelection)
        {
            return VsUtility.GetIsSolutionNodeSelected(vsMonitorSelection);
        }
    }
}