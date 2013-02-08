using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio
{
    internal static class VsHierarchyHelper
    {
        public static IDictionary<string, ISet<VsHierarchyItem>> GetAllExpandedNodes(ISolutionManager solutionManager)
        {
            // this operation needs to execute on UI thread
            return ThreadHelper.Generic.Invoke(() =>
                {
                    var results = new Dictionary<string, ISet<VsHierarchyItem>>(StringComparer.OrdinalIgnoreCase);
                    foreach (Project project in solutionManager.GetProjects())
                    {
                        ICollection<VsHierarchyItem> expandedNodes =
                            GetExpandedProjectHierarchyItems(project);
                        Debug.Assert(!results.ContainsKey(project.GetUniqueName()));
                        results[project.GetUniqueName()] =
                            new HashSet<VsHierarchyItem>(expandedNodes);
                    }
                    return results;
                }
            );
        }

        public static void CollapseAllNodes(ISolutionManager solutionManager, IDictionary<string, ISet<VsHierarchyItem>> ignoreNodes)
        {
            // this operation needs to execute on UI thread
            ThreadHelper.Generic.Invoke(() =>
                {
                    foreach (Project project in solutionManager.GetProjects())
                    {
                        ISet<VsHierarchyItem> expandedNodes;
                        if (ignoreNodes.TryGetValue(project.GetUniqueName(), out expandedNodes) &&
                            expandedNodes != null)
                        {
                            CollapseProjectHierarchyItems(project, expandedNodes);
                        }
                    }
                });
        }

        private static ICollection<VsHierarchyItem> GetExpandedProjectHierarchyItems(EnvDTE.Project project)
        {
            VsHierarchyItem projectHierarchyItem = GetHierarchyItemForProject(project);
            IVsUIHierarchyWindow solutionExplorerWindow = GetSolutionExplorerHierarchyWindow();

            if (solutionExplorerWindow == null)
            {
                // If the solution explorer is collapsed since opening VS, this value is null. In such a case, simply exit early.
                return new VsHierarchyItem[0];
            }

            var expandedItems = new List<VsHierarchyItem>();

            // processCallback return values: 
            //     0   continue, 
            //     1   don't recurse into, 
            //    -1   stop
            projectHierarchyItem.WalkDepthFirst(
                fVisible: true, 
                processCallback:
                            (VsHierarchyItem vsItem, object callerObject, out object newCallerObject) =>
                            {
                                newCallerObject = null;
                                if (IsVsHierarchyItemExpanded(vsItem, solutionExplorerWindow))
                                {
                                    expandedItems.Add(vsItem);
                                }
                                return 0;
                            },
                callerObject: null);

            return expandedItems;
        }

        private static void CollapseProjectHierarchyItems(Project project, ISet<VsHierarchyItem> ignoredHierarcyItems)
        {
            VsHierarchyItem projectHierarchyItem = GetHierarchyItemForProject(project);
            IVsUIHierarchyWindow solutionExplorerWindow = GetSolutionExplorerHierarchyWindow();

            if (solutionExplorerWindow == null)
            {
                // If the solution explorer is collapsed since opening VS, this value is null. In such a case, simply exit early.
                return;
            }

            // processCallback return values:
            //     0   continue, 
            //     1   don't recurse into, 
            //    -1   stop
            projectHierarchyItem.WalkDepthFirst(
                fVisible: true, 
                processCallback:
                            (VsHierarchyItem currentHierarchyItem, object callerObject, out object newCallerObject) =>
                            {
                                newCallerObject = null;
                                if (!ignoredHierarcyItems.Contains(currentHierarchyItem))
                                {
                                    CollapseVsHierarchyItem(currentHierarchyItem, solutionExplorerWindow);
                                }
                                return 0;
                            },
                callerObject: null);
        }

        private static VsHierarchyItem GetHierarchyItemForProject(EnvDTE.Project project)
        {
            IVsHierarchy hierarchy;

            // Get the solution
            IVsSolution solution = ServiceLocator.GetGlobalService<SVsSolution, IVsSolution>();
            int hr = solution.GetProjectOfUniqueName(project.GetUniqueName(), out hierarchy);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return new VsHierarchyItem(hierarchy);
        }

        private static void CollapseVsHierarchyItem(VsHierarchyItem vsHierarchyItem, IVsUIHierarchyWindow vsHierarchyWindow)
        {
            if (vsHierarchyItem == null || vsHierarchyWindow == null)
            {
                return;
            }

            vsHierarchyWindow.ExpandItem(vsHierarchyItem.UIHierarchy(), vsHierarchyItem.VsItemID, EXPANDFLAGS.EXPF_CollapseFolder);
        }

        private static bool IsVsHierarchyItemExpanded(VsHierarchyItem hierarchyItem, IVsUIHierarchyWindow uiWindow)
        {
            if (!hierarchyItem.IsExpandable())
            {
                return false;
            }

            const uint expandedStateMask = (uint)__VSHIERARCHYITEMSTATE.HIS_Expanded;
            uint itemState;

            uiWindow.GetItemState(hierarchyItem.UIHierarchy(), hierarchyItem.VsItemID, expandedStateMask, out itemState);
            return ((__VSHIERARCHYITEMSTATE)itemState == __VSHIERARCHYITEMSTATE.HIS_Expanded);
        }

        private static IVsUIHierarchyWindow GetSolutionExplorerHierarchyWindow()
        {
            return VsShellUtilities.GetUIHierarchyWindow(
                ServiceLocator.GetInstance<IServiceProvider>(),
                new Guid(VsConstants.VsWindowKindSolutionExplorer));
        }
    }
}