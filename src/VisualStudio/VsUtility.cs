using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace NuGet.VisualStudio
{
    // The class is also added as a link to VsEvents project
    // Don't add any code in this class that will result in loading of NuGet.VisualStudio.dll from VsEvents
    internal static class VsUtility
    {
        public const string NuGetSolutionSettingsFolder = ".nuget";
        public const string PackageReferenceFile = "packages.config";

        private static readonly HashSet<string> _supportedProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        {
            VsConstants.WebSiteProjectTypeGuid, 
            VsConstants.CsharpProjectTypeGuid, 
            VsConstants.VbProjectTypeGuid,
            VsConstants.CppProjectTypeGuid,
            VsConstants.JsProjectTypeGuid,
            VsConstants.FsharpProjectTypeGuid,
            VsConstants.NemerleProjectTypeGuid,
            VsConstants.WixProjectTypeGuid,
            VsConstants.SynergexProjectTypeGuid,
            VsConstants.NomadForVisualStudioProjectTypeGuid 
        };

        /// <summary>
        /// Returns the full path of the project directory.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The full path of the project directory.</returns>
        public static string GetFullPath(Project project)
        {
            Debug.Assert(project != null);
            if (project.IsUnloaded())
            {
                // To get the directory of an unloaded project, we use the UniqueName property,
                // which is the path of the project file relative to the solution directory.
                var solutionDirectory = Path.GetDirectoryName(project.DTE.Solution.FullName);
                var projectFileFullPath = Path.Combine(solutionDirectory, project.UniqueName);
                return Path.GetDirectoryName(projectFileFullPath);
            }

            string fullPath = GetPropertyValue<string>(project, "FullPath");
            if (!String.IsNullOrEmpty(fullPath))
            {
                // Some Project System implementations (JS metro app) return the project 
                // file as FullPath. We only need the parent directory
                if (File.Exists(fullPath))
                {
                    fullPath = Path.GetDirectoryName(fullPath);
                }
            }
            else
            {
                // C++ projects do not have FullPath property, but do have ProjectDirectory one.
                fullPath = GetPropertyValue<string>(project, "ProjectDirectory");
            }

            return fullPath;
        }

        public static bool IsSupported(Project project)
        {
            Debug.Assert(project != null);
            return project.Kind != null && _supportedProjectTypes.Contains(project.Kind);
        }

        public static T GetPropertyValue<T>(Project project, string propertyName)
        {
            Debug.Assert(project != null);
            if (project.Properties == null)
            {
                // this happens in unit tests
                return default(T);
            }

            try
            {
                Property property = project.Properties.Item(propertyName);
                if (property != null)
                {
                    // REVIEW: Should this cast or convert?
                    return (T)property.Value;
                }
            }
            catch (ArgumentException)
            {
            }
            return default(T);
        }

        /// <summary>
        /// Gets the EnvDTE.Project instance from IVsHierarchy
        /// </summary>
        /// <param name="pHierarchy">pHierarchy is the IVsHierarchy instance from which the project instance is obtained</param>
        public static Project GetProjectFromHierarchy(IVsHierarchy pHierarchy)
        {
            Debug.Assert(pHierarchy != null);

            // Set it to null to avoid unassigned local variable warning
            Project project = null;
            object projectObject;

            if (pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectObject) >= 0)
            {
                project = (Project)projectObject;
            }

            return project;
        }

        /// <summary>
        /// Gets the path to .nuget folder present in the solution
        /// </summary>
        /// <param name="solution">Solution from which .nuget folder's path is obtained</param>
        public static string GetNuGetSolutionFolder(Solution solution)
        {
            Debug.Assert(solution != null);
            var solutionDirectory = Path.GetDirectoryName(solution.FullName);
            return Path.Combine(solutionDirectory, NuGetSolutionSettingsFolder);
        }

        /// <summary>
        /// Returns true if the project has the packages.config file
        /// </summary>
        /// <param name="project">Project under whose directory packages.config is searched for</param>
        public static bool PackagesConfigExists(Project project)
        {
            var packageReferenceFileName = GetPackageReferenceFileFullPath(project);

            // Here we just check if the packages.config file exists instead of 
            // calling IsNuGetInUse because that will cause NuGet.VisualStudio.dll to get loaded.
            bool isUnloaded = VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
            if ((isUnloaded || IsSupported(project)) && File.Exists(packageReferenceFileName))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the full path of the packages config file associated with the project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The full path of the packages config file.</returns>
        public static string GetPackageReferenceFileFullPath(Project project)
        {
            Debug.Assert(project != null);
            var projectDirectory = GetFullPath(project);

            var packageReferenceFileName = Path.Combine(
                projectDirectory ?? String.Empty,
                PackageReferenceFile);

            return packageReferenceFileName;
        }

        public static void ShowError(ErrorListProvider errorListProvider, TaskErrorCategory errorCategory, TaskPriority priority, string errorText, IVsHierarchy hierarchyItem)
        {
            ErrorTask retargetErrorTask = new ErrorTask();
            retargetErrorTask.Text = errorText;
            retargetErrorTask.ErrorCategory = errorCategory;
            retargetErrorTask.Category = TaskCategory.BuildCompile;
            retargetErrorTask.Priority = priority;
            retargetErrorTask.HierarchyItem = hierarchyItem;
            errorListProvider.Tasks.Add(retargetErrorTask);
            errorListProvider.BringToFront();
            errorListProvider.ForceShowErrors();
        }

        public static Project GetActiveProject(IVsMonitorSelection vsMonitorSelection)
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                vsMonitorSelection.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC);

                if (ppHier == IntPtr.Zero)
                {
                    return null;
                }

                // multiple items are selected.
                if (pitemid == (uint)VSConstants.VSITEMID.Selection)
                {
                    return null;
                }

                IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) as IVsHierarchy;
                if (hierarchy != null)
                {
                    object project;
                    if (hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project) >= 0)
                    {
                        return (Project)project;
                    }
                }

                return null;
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }
        }

        public static bool GetIsSolutionNodeSelected(IVsMonitorSelection vsMonitorSelection)
        {
            IntPtr ppHier = IntPtr.Zero;
            uint pitemid;
            IVsMultiItemSelect ppMIS;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                vsMonitorSelection.GetCurrentSelection(out ppHier, out pitemid, out ppMIS, out ppSC);
                if (pitemid == (uint)VSConstants.VSITEMID.Root)
                {
                    if (ppHier == IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }

            return false;
        }
    }
}
